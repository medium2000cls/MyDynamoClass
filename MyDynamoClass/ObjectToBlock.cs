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
            Document ac_doc = doc_dyn.AcDocument;
            Database ac_db = ac_doc.Database;
            Editor ac_ed = ac_doc.Editor;

            string block_handle = "";

            using (DocumentLock acLckDoc = ac_doc.LockDocument())
            {
                using (Transaction acTrans = ac_db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Открыть таблицу блоков для чтения
                        BlockTable acBlkTbl = acTrans.GetObject(ac_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        
                        
                        // создаем новое определение блока
                        BlockTableRecord acBlkTblRec = new BlockTableRecord();
                        acBlkTblRec.Name = block_name;

                        acBlkTbl.UpgradeOpen();

                        // добавляем его в таблицу блоков
                        ObjectId btrId = acBlkTbl.Add(acBlkTblRec);
                        acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);

                        ObjectIdCollection ids = new ObjectIdCollection();

                        foreach (var handle in object_handles)
                        {
                            //Получаем ObjectId
                            long obj_handle = Convert.ToInt64(handle, 16);
                            ObjectId id = ac_db.GetObjectId(false, new Handle(obj_handle), 0);
                            ids.Add(id);
                            //Entity entity = acTrans.GetObject(id, OpenMode.ForWrite) as Entity;
                        }
                        
                        acBlkTblRec.AssumeOwnershipOf(ids);
                        //acTrans.AddNewlyCreatedDBObject(entity, true);
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
    }
}