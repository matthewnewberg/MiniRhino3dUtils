using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace EventWatcherMeshUpdate
{
    [System.Runtime.InteropServices.Guid("4281794f-0918-45da-b0a8-ad8550bb4e73")]
    public class MeshLineThroughPtsCommand : Command
    {
        static MeshLineThroughPtsCommand _instance;
        public MeshLineThroughPtsCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MeshLineThroughPtsCommand command.</summary>
        public static MeshLineThroughPtsCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "MeshLineThroughPts"; }
        }

        private const int HISTORY_VERSION = 201700903;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType filter = Rhino.DocObjects.ObjectType.Mesh;
            Rhino.DocObjects.ObjRef objref;
            Rhino.Commands.Result rc = Rhino.Input.RhinoGet.GetOneObject("Select mesh to add line to", false, filter, out objref);
            if (rc != Rhino.Commands.Result.Success || objref == null)
                return rc;

            Rhino.Geometry.Mesh mesh = objref.Mesh();
            if (null == mesh || !mesh.IsValid)
                return Rhino.Commands.Result.Failure;

            Line fittedLine = new Line();
            Line.TryFitLineToPoints(mesh.Vertices.ToPoint3dArray(), out fittedLine);

            Rhino.DocObjects.HistoryRecord history = new Rhino.DocObjects.HistoryRecord(this, HISTORY_VERSION);
            WriteHistory(history, objref);
            
            doc.Objects.AddCurve(new LineCurve(fittedLine), null, history, false);

            doc.Views.Redraw();

            return Result.Success;
        }


        protected override bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replay)
        {
            Rhino.DocObjects.ObjRef objref = null;

            if (!ReadHistory(replay, ref objref))
                return false;

            Rhino.Geometry.Mesh mesh = objref.Mesh();
            if (null == mesh)
                return false;

            if (replay.Results.Length != 1)
                return false;

            Line fittedLine = new Line();
            Line.TryFitLineToPoints(mesh.Vertices.ToPoint3dArray(), out fittedLine);

            replay.Results[0].UpdateToCurve(new LineCurve(fittedLine), null);

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
