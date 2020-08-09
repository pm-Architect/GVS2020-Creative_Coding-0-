using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(double SegmentLength, int SegmentCount, Point3d TargetPoint, bool Reset, ref object LinesOut)
  {
        if (initialized && (!Reset))
    {
      // Do nothing
    }
    else
    {
      rArm = new RobotArm(SegmentCount, SegmentLength, TargetPoint);
      initialized = true;
    }
    rArm.TargetPoint = TargetPoint;
    rArm.Update();
    LinesOut = rArm.Show();
  }

  // <Custom additional code> 
    public bool initialized = false;
  public RobotArm rArm;

  public class RobotArm
  {
    // Properties
    public List<Segment> Segments {get; set;}
    public Point3d TargetPoint {get; set;}

    // Constructor
    public RobotArm(int numSeg, double segLen, Point3d targetPt)
    {
      this.Segments = new List<Segment>();
      // for (variables; condition; change;)
      for (var x = 0; x < numSeg; x++)
      {
        // Add a segment
        Segment seg = new Segment(segLen, x);
        this.Segments.Add(seg);
        // Check whether the segment is the first segment
        if (x == 0)
        {
          // Do nothing
        }
        else
        {
          // If not, assign a parent segment to follow
          seg.ParentSegment = this.Segments[(x - 1)];
        }

      }
      this.TargetPoint = targetPt;
    }

    // Functions
    public void Update()
    {
      // For each Segment, call Segment.Update();
      for (var i = 0; i < this.Segments.Count; i++)
      {
        if (i == 0)
        {
          this.Segments[i].B = (this.TargetPoint - Point3d.Origin);
          this.Segments[i].A = this.Segments[i + 1].A;
        }
        else if (i == (this.Segments.Count - 1))
        {
          this.Segments[i].B = new Vector3d(0, 0, 0);
          this.Segments[i].Update();
        }
        else
        {
          this.Segments[i].B = this.Segments[i].A + this.Segments[i].Update();
        }
      }
    }

    public List<Line> Show()
    {
      List<Line> lOut = new List<Line>();
      // For each Segment, call Segment.Show();
      for (var i = 0; i < this.Segments.Count; i++)
      {
        lOut.Add(this.Segments[i].Show());
      }
      return lOut;
    }

  }

  public class Segment
  {
    // Properties
    public Vector3d A {get; set;}
    public Vector3d B {get; set;}
    public double Length {get; set;}
    public int Index {get; set;}
    public Segment ParentSegment {get; set;}

    // Constructor
    public Segment(double len, int ind)
    {
      this.Length = len;
      this.Index = ind;
      this.ParentSegment = null;
    }

    // Functions
    public Vector3d Update()
    {
      Vector3d bTarget = this.ParentSegment.B;
      // bTarget - this.A
      Vector3d direction = Vector3d.Subtract(bTarget, this.A);
      direction.Unitize();
      direction = Vector3d.Multiply(direction, (this.Length * (-1)));
      this.A = Vector3d.Add(bTarget, direction);
      return direction;
    }

    public void FollowParent()
    {

    }

    public Line Show()
    {
      return new Line((Point3d.Origin + this.A), (Point3d.Origin + this.B));
    }

  }
  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        double SegmentLength = default(double);
    if (inputs[0] != null)
    {
      SegmentLength = (double)(inputs[0]);
    }

    int SegmentCount = default(int);
    if (inputs[1] != null)
    {
      SegmentCount = (int)(inputs[1]);
    }

    Point3d TargetPoint = default(Point3d);
    if (inputs[2] != null)
    {
      TargetPoint = (Point3d)(inputs[2]);
    }

    bool Reset = default(bool);
    if (inputs[3] != null)
    {
      Reset = (bool)(inputs[3]);
    }



    //3. Declare output parameters
      object LinesOut = null;


    //4. Invoke RunScript
    RunScript(SegmentLength, SegmentCount, TargetPoint, Reset, ref LinesOut);
      
    try
    {
      //5. Assign output parameters to component...
            if (LinesOut != null)
      {
        if (GH_Format.TreatAsCollection(LinesOut))
        {
          IEnumerable __enum_LinesOut = (IEnumerable)(LinesOut);
          DA.SetDataList(1, __enum_LinesOut);
        }
        else
        {
          if (LinesOut is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(LinesOut));
          }
          else
          {
            //assign direct
            DA.SetData(1, LinesOut);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}