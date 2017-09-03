using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace EventWatcherMeshUpdate
{
    [System.Runtime.InteropServices.Guid("60f120f0-f729-4cf9-8768-29c01447d0d0")]
    public class miniMeshLineThroughPtsCommand : Command
    {
        bool Enabled = false;

        bool inReplace = false;

        Dictionary<Guid, Guid> objectLookup = new Dictionary<Guid, Guid>();

        static miniMeshLineThroughPtsCommand _instance;
        public miniMeshLineThroughPtsCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MeshLineThroughPts command.</summary>
        public static miniMeshLineThroughPtsCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "miniMeshLineThroughPts"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            Enabled = !Enabled;

            if (Enabled)
            {
                RhinoDoc.AddRhinoObject += RhinoObjectAdded;
                RhinoDoc.DeleteRhinoObject += RhinoObjectDeleted;
                RhinoDoc.ReplaceRhinoObject += RhinoObjectReplace;

                foreach (var obj in doc.Objects)
                {
                    if (!obj.IsDeleted)
                        continue;

                    var mesh = obj.Geometry as Mesh;
                    if (mesh != null && mesh.IsValid)
                    {
                        AddThroughPts(obj, obj.Attributes.ObjectId, doc);

                    }
                }
                
            }
            else
            {
                RhinoDoc.AddRhinoObject -= RhinoObjectAdded;
                RhinoDoc.DeleteRhinoObject -= RhinoObjectDeleted;
                RhinoDoc.ReplaceRhinoObject -= RhinoObjectReplace;
            }

            RhinoApp.WriteLine("miniMeshLineThroughPts:" + Enabled);

            doc.Views.Redraw();

            return Result.Success;
        }


        private void RhinoObjectDeleted(Object sender, RhinoObjectEventArgs e)
        {
            if (inReplace)
                return;

            if (objectLookup.ContainsKey(e.ObjectId))
            {
                e.TheObject.Document.Objects.Delete(objectLookup[e.ObjectId], true);

                objectLookup.Remove(e.ObjectId);
            }
        }

        private void RhinoObjectReplace(object sender, RhinoReplaceObjectEventArgs e)
        {
            inReplace = true;
        }

        private void RhinoObjectAdded(Object sender, RhinoObjectEventArgs e)
        {
            var doc = e.TheObject.Document;
            if (Enabled)
            {
                AddThroughPts(e.TheObject, e.ObjectId, doc);
            }

            inReplace = false;
        }

        private void AddThroughPts(Rhino.DocObjects.RhinoObject obj, Guid objectId,  RhinoDoc doc)
        {
            Rhino.DocObjects.MeshObject meshObject = obj as MeshObject;

            if (meshObject != null && meshObject.IsValid)
            {
                var objectMesh = obj.Geometry as Mesh;

                Line fittedLine = new Line();
                Line.TryFitLineToPoints(objectMesh.Vertices.ToPoint3dArray(), out fittedLine);

                bool replacedResult = false;

                if (objectLookup.ContainsKey(objectId))
                {
                    replacedResult = doc.Objects.Replace(objectLookup[objectId], new LineCurve(fittedLine));
                    doc.Objects.ModifyAttributes(objectLookup[objectId], obj.Attributes, true);
                }

                if (!replacedResult)
                    objectLookup[objectId] = doc.Objects.Add(new LineCurve(fittedLine), obj.Attributes);

            }
        }
    }
}
