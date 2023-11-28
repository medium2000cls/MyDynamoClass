using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Colors = Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DataShortcuts;
using Autodesk.DesignScript.Runtime;
using DBObject = Autodesk.Civil.DatabaseServices.DBObject;
using ds = Autodesk.DesignScript.Geometry;
using dyn = Autodesk.AutoCAD.DynamoNodes;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Exception = System.Exception;

namespace MyDynamoClass
{
    public class AcadBlock
    {
        public static string HandleToBlock(Autodesk.AutoCAD.DynamoNodes.Document doc_dyn, List<string> object_handles,
            string block_name)
        {
            Document acDoc = doc_dyn.AcDocument;
            Database acDb = acDoc.Database;
            Editor ac_ed = acDoc.Editor;

            string block_handle = "";

            using (DocumentLock acLckDoc = acDoc.LockDocument())
            {
                using (Transaction acTrans = acDb.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Открыть таблицу блоков для чтения
                        BlockTable acBlkTbl = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;


                        // создаем новое определение блока
                        BlockTableRecord acBlkTblRec = new BlockTableRecord();
                        acBlkTblRec.Name = block_name;

                        acBlkTbl.UpgradeOpen();

                        // добавляем его в таблицу блоков
                        ObjectId btrId = acBlkTbl.Add(acBlkTblRec);
                        //acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);

                        ObjectIdCollection ids = new ObjectIdCollection();

                        foreach (var handle in object_handles)
                        {
                            //Получаем ObjectId
                            long obj_handle = Convert.ToInt64(handle, 16);
                            ObjectId id = acDb.GetObjectId(false, new Handle(obj_handle), 0);
                            ids.Add(id);
                            //Entity entity = acTrans.GetObject(id, OpenMode.ForWrite) as Entity;
                        }

                        acBlkTblRec.AssumeOwnershipOf(ids);
                        acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);

                        block_handle = acBlkTblRec.Handle.ToString();
                        acTrans.Commit();
                    }
                    catch (Exception e)
                    {
                        block_handle += e.ToString();
                    }
                }
            }

            return block_handle;
        }

        public static List<string> HandleToBlocks(Autodesk.AutoCAD.DynamoNodes.Document doc_dyn,
            List<List<string>> object_handles,
            List<string> block_names)
        {
            List<string> blockHandles = new List<string>();
            if (object_handles.Count == block_names.Count)
            {
                int listCount = object_handles.Count;
                for (int i = 0; i < listCount; ++i)
                {
                    var handle = HandleToBlock(doc_dyn, object_handles[i], block_names[i]);
                    blockHandles.Add(handle);
                }
            }
            else
            {
                throw new ArgumentException("Object count not equal to name count.");
            }

            return blockHandles;
        }

        public static List<string> HandleToBlockReference(Autodesk.AutoCAD.DynamoNodes.Document doc_dyn,
            List<string> object_handles)
        {
            Document acDoc = doc_dyn.AcDocument;
            Database acDb = acDoc.Database;
            Editor acEd = acDoc.Editor;

            List<string> blockReferenceHandle = new List<string>();

            using (DocumentLock acLckDoc = acDoc.LockDocument())
            {
                using (Transaction acTrans = acDb.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Открыть таблицу блоков для чтения
                        BlockTable acBlkTbl = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForWrite) as BlockTable;

                        // теперь создадим экземпляр блока на чертеже
                        // получаем пространство модели
                        BlockTableRecord btrModelSpace =
                            acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as
                                BlockTableRecord;
                        foreach (var handle in object_handles)
                        {
                            long objHandle = Convert.ToInt64(handle, 16);
                            var s = new Handle(objHandle);
                            s.ToString();
                            ObjectId id = acDb.GetObjectId(false, new Handle(objHandle), 0);
                            // создаем новый экземпляр блока на основе его определения
                            BlockReference brRefBlock = new BlockReference(Point3d.Origin, id);

                            // добавляем экземпляр блока в базу данных пространства модели
                            ObjectId blockReferenceId = btrModelSpace.AppendEntity(brRefBlock);
                            blockReferenceHandle.Add(blockReferenceId.Handle.ToString());
                            acTrans.AddNewlyCreatedDBObject(brRefBlock, true);
                        }

                        acTrans.Commit();
                    }
                    catch (Exception e)
                    {
                        blockReferenceHandle.Add(e.ToString());
                    }
                }
            }

            return blockReferenceHandle;
        }

        public static void HandleToSaveBlock(Autodesk.AutoCAD.DynamoNodes.Document doc_dyn, List<string> object_handles, string full_name)
        {
            Document acDoc = doc_dyn.AcDocument;
            Database acDb = acDoc.Database;
            using (DocumentLock acLckDoc = acDoc.LockDocument()) {
                using (Transaction acTrans = acDb.TransactionManager.StartTransaction()) {
                    BlockTable acBlkTbl = acTrans.GetObject(acDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    
                    Database acDbNew = new Database();
                    ObjectIdCollection blockIds = new ObjectIdCollection();

                    foreach (var handle in object_handles)
                    {
                        Handle objHandle = new Handle(Convert.ToInt64(handle, 16));
                        ObjectId id_block = acDb.GetObjectId(false, objHandle, 0);
                        blockIds.Add(id_block);
                    }
                    IdMapping mapping = new IdMapping();
                    acDbNew.WblockCloneObjects(blockIds,acDbNew.CurrentSpaceId, mapping, DuplicateRecordCloning.Replace, false);
                    acDbNew.SaveAs(full_name, DwgVersion.Current);
                    acTrans.Commit();
                }
            }
        }
    }
}