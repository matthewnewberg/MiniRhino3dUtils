using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;


namespace EventWatcherMeshUpdate
{
    [System.Runtime.InteropServices.Guid("84299824-d683-4468-b1de-1385f4e23637")]
    public class miniMeshFaceCenters : Command
    {
        bool Enabled = false;
        bool inReplace = false;

        Dictionary<Guid, List<Guid>> objectLookup = new Dictionary<Guid, List<Guid>>();

        static miniMeshFaceCenters _instance;
        public miniMeshFaceCenters()
        {
            _instance = this;
        }

        ///<summary>The only instance of the miniMeshFaceCenters command.</summary>
        public static miniMeshFaceCenters Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "miniMeshFaceCenters"; }
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
                    if (obj.IsDeleted)
                        continue;

                    var mesh = obj.Geometry as Mesh;
                    if (mesh != null && mesh.IsValid)
                    {
                        AddFaceCenters(obj, obj.Attributes.ObjectId, doc);
                    }
                }

            }
            else
            {
                RhinoDoc.AddRhinoObject -= RhinoObjectAdded;
                RhinoDoc.DeleteRhinoObject -= RhinoObjectDeleted;
                RhinoDoc.ReplaceRhinoObject -= RhinoObjectReplace;

                // Delete all the points being shown, and clear the lookup
                foreach (var objects in objectLookup)
                    doc.Objects.Delete(objects.Value, true);

                objectLookup.Clear();
            }

            RhinoApp.WriteLine("miniMeshFaceCenters:" + Enabled);

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
                AddFaceCenters(e.TheObject, e.ObjectId, doc);
            }

            inReplace = false;
        }

        List<Point3d> GetFaceCenters(Mesh mesh)
        {

            var res = new List<Point3d>();
            foreach (var f in mesh.Faces)
            {
                if (f.IsQuad)
                {
                    Point3d faceCenter = new Point3d();
                    faceCenter.X = mesh.Vertices[f.A].X / 4 + mesh.Vertices[f.B].X / 4 + mesh.Vertices[f.C].X / 4 + mesh.Vertices[f.D].X / 4;
                    faceCenter.Y = mesh.Vertices[f.A].Y / 4 + mesh.Vertices[f.B].Y / 4 + mesh.Vertices[f.C].Y / 4 + mesh.Vertices[f.D].Y / 4;
                    faceCenter.Z = mesh.Vertices[f.A].Z / 4 + mesh.Vertices[f.B].Z / 4 + mesh.Vertices[f.C].Z / 4 + mesh.Vertices[f.D].Z / 4;
                    res.Add(faceCenter);
                }
                else
                {
                    Point3d faceCenter = new Point3d();
                    faceCenter.X = mesh.Vertices[f.A].X / 3 + mesh.Vertices[f.B].X / 3 + mesh.Vertices[f.C].X / 3;
                    faceCenter.Y = mesh.Vertices[f.A].Y / 3 + mesh.Vertices[f.B].Y / 3 + mesh.Vertices[f.C].Y / 3;
                    faceCenter.Z = mesh.Vertices[f.A].Z / 3 + mesh.Vertices[f.B].Z / 3 + mesh.Vertices[f.C].Z / 3;
                    res.Add(faceCenter);
                }
            }

            return res;
        }

        private void AddFaceCenters(Rhino.DocObjects.RhinoObject obj, Guid objectId, RhinoDoc doc)
        {
            Rhino.DocObjects.MeshObject meshObject = obj as MeshObject;

            if (meshObject != null && meshObject.IsValid)
            {
                var objectMesh = obj.Geometry as Mesh;

                bool replacedResult = false;

                List<Point3d> faceCenters = GetFaceCenters(objectMesh);

                if (objectLookup.ContainsKey(objectId))
                {
                    List<Guid> oldObjects = objectLookup[objectId];

                    int i = 0; 
                    if (oldObjects.Count == faceCenters.Count)
                    {
                        foreach(var p in oldObjects)
                            replacedResult &= doc.Objects.Replace(p, faceCenters[i++]);
                    }
                    else
                    {
                        foreach (var p in oldObjects)
                            doc.Objects.Delete(p, true);
                    }
                }

                if (!replacedResult)
                {
                    objectLookup[objectId] = new List<Guid>();
                    foreach (var f in faceCenters)
                        objectLookup[objectId].Add(doc.Objects.AddPoint(f, obj.Attributes));
                }

            }
        }
    }
}
