using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.DocObjects;


namespace EventWatcherMeshUpdate
{

    [System.Runtime.InteropServices.Guid("6634a1fb-7203-47d6-927e-26decc2dcd9f")]
    public class miniSlopOnCylinderCommand : Command
    {
        Rhino.Geometry.Morphs.SplopSpaceMorph morph;
        Rhino.Geometry.BoundingBox testBox;

        public miniSlopOnCylinderCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;

            Rhino.Geometry.Circle circle0 = new Circle(Plane.WorldZX, 17.3 / 2.0);
            circle0.Translate(new Vector3d(0, 30, 0));
            circle0.Reverse();

            var curve = circle0.ToNurbsCurve();

            curve.Rotate(Rhino.RhinoMath.ToRadians(180), Vector3d.YAxis, Point3d.Origin);

            var surface = Rhino.Geometry.Surface.CreateExtrusion(curve, new Vector3d(0, 50, 0));

            surface.SetDomain(0, new Interval(0, 1));
            surface.SetDomain(1, new Interval(0, 1));

            morph = new Rhino.Geometry.Morphs.SplopSpaceMorph(Plane.WorldXY, surface, new Point2d(.5, .5));

            testBox = new BoundingBox(-50, -25, -50, 50, 25, 50);
        }

        ///<summary>The only instance of this command.</summary>
        public static miniSlopOnCylinderCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "miniSlopOnCylinder"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            Enabled = !Enabled;

            if (Enabled)
            {
                RhinoDoc.AddRhinoObject += RhinoObjectAdded;
                RhinoDoc.DeleteRhinoObject += RhinoObjectDeleted;
                RhinoDoc.ReplaceRhinoObject += RhinoObjectReplace;
            }

            else
            {
                RhinoDoc.AddRhinoObject -= RhinoObjectAdded;
                RhinoDoc.DeleteRhinoObject -= RhinoObjectDeleted;
                RhinoDoc.ReplaceRhinoObject -= RhinoObjectReplace;
            }


            RhinoApp.WriteLine("miniSlopOnCylinder:" + Enabled);

            return Result.Success;
        }

        bool Enabled = false;

        bool inReplace = false;

        Dictionary<Guid, Guid> objectLookup = new Dictionary<Guid, Guid>();

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
                Rhino.DocObjects.MeshObject meshObject = e.TheObject as MeshObject;

                if (meshObject != null && meshObject.IsValid)
                {
                    var objectBox = e.TheObject.Geometry.GetBoundingBox(true);

                    if (!testBox.Contains(objectBox, true))
                        return;

                    var mesh = meshObject.Geometry as Mesh;

                    var splopMesh = mesh.DuplicateMesh();

                    morph.Morph(splopMesh);

                    bool replacedResult = false;

                    if (objectLookup.ContainsKey(e.ObjectId))
                    {
                        replacedResult = doc.Objects.Replace(objectLookup[e.ObjectId], splopMesh);
                        doc.Objects.ModifyAttributes(objectLookup[e.ObjectId], e.TheObject.Attributes, true);
                    }

                    if (!replacedResult)
                        objectLookup[e.ObjectId] = doc.Objects.Add(splopMesh, e.TheObject.Attributes);
                }
            }

            inReplace = false;
        }
    }
}
