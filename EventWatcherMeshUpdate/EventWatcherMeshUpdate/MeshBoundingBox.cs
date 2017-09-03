using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace EventWatcherMeshUpdate
{
    [System.Runtime.InteropServices.Guid("f38fae3b-b71e-4799-86b9-e373ec4e222f")]
    public class MeshBoundingBox : Command
    {
        static MeshBoundingBox _instance;
        public MeshBoundingBox()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MeshBoundingBox command.</summary>
        public static MeshBoundingBox Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "MeshBoundingBox"; }
        }

        private const int HISTORY_VERSION = 201700903;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.AnyObject;
            Rhino.DocObjects.ObjRef objref;
            Rhino.Commands.Result rc = Rhino.Input.RhinoGet.GetOneObject("Select object to create mesh bounding box", false, filter, out objref);
            if (rc != Rhino.Commands.Result.Success || objref == null)
                return rc;

            var geom = objref.Geometry();
            if (null == geom || !geom.IsValid)
                return Rhino.Commands.Result.Failure;

            var mesh = MeshBoundingBoxFromObject(objref.Geometry());
            
            Rhino.DocObjects.HistoryRecord history = new Rhino.DocObjects.HistoryRecord(this, HISTORY_VERSION);
            WriteHistory(history, objref);

            doc.Objects.AddMesh(mesh, null, history, false);

            doc.Views.Redraw();

            return Result.Success;
        }

        Mesh MeshBoundingBoxFromObject(GeometryBase obj)
        {
            BoundingBox box = obj.GetBoundingBox(true);
            return Mesh.CreateFromBox(box, 2, 2, 2);
        }

        protected override bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replay)
        {
            Rhino.DocObjects.ObjRef objref = null;

            if (!ReadHistory(replay, ref objref))
                return false;

            var obj = objref.Geometry();
            if (null == obj)
                return false;

            if (replay.Results.Length != 1)
                return false;

            Mesh mesh = MeshBoundingBoxFromObject(obj);

            replay.Results[0].UpdateToMesh(mesh, null);

            return true;
        }

        private bool WriteHistory(Rhino.DocObjects.HistoryRecord history, Rhino.DocObjects.ObjRef objref)
        {
            if (!history.SetObjRef(0, objref))
                return false;

            return true;
        }

        private bool ReadHistory(Rhino.DocObjects.ReplayHistoryData replay, ref Rhino.DocObjects.ObjRef objref)
        {
            if (HISTORY_VERSION != replay.HistoryVersion)
                return false;

            objref = replay.GetRhinoObjRef(0);
            if (null == objref)
                return false;

            return true;
        }
    }
}
