using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Echo.Core.Common.Packed;

using EB = EditorBrowsableAttribute;
using DB = DebuggerBrowsableAttribute;
using EBS = EditorBrowsableState;
using DBS = DebuggerBrowsableState;
//
using F2 = Float2;
using F3 = Float3;
using F4 = Float4;
//
using I2 = Int2;
using I3 = Int3;
using I4 = Int4;

partial struct Float2
{

#region Four

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXX => new(X, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXY => new(X, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXX_ => new(X, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYX => new(X, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYY => new(X, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXY_ => new(X, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_X => new(X, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_Y => new(X, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX__ => new(X, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXX => new(X, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXY => new(X, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYX_ => new(X, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYX => new(X, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYY => new(X, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYY_ => new(X, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_X => new(X, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_Y => new(X, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY__ => new(X, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XX => new(X, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XY => new(X, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_X_ => new(X, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YX => new(X, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YY => new(X, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_Y_ => new(X, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X__X => new(X, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__Y => new(X, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X___ => new(X, 0f, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXX => new(Y, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXY => new(Y, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXX_ => new(Y, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYX => new(Y, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYY => new(Y, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXY_ => new(Y, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_X => new(Y, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_Y => new(Y, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX__ => new(Y, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXX => new(Y, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXY => new(Y, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYX_ => new(Y, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYX => new(Y, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYY => new(Y, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYY_ => new(Y, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_X => new(Y, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_Y => new(Y, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY__ => new(Y, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XX => new(Y, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XY => new(Y, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_X_ => new(Y, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YX => new(Y, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YY => new(Y, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_Y_ => new(Y, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__X => new(Y, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__Y => new(Y, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y___ => new(Y, 0f, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXX => new(0f, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXY => new(0f, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XX_ => new(0f, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYX => new(0f, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYY => new(0f, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XY_ => new(0f, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_X => new(0f, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_Y => new(0f, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X__ => new(0f, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXX => new(0f, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXY => new(0f, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YX_ => new(0f, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYX => new(0f, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYY => new(0f, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YY_ => new(0f, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_X => new(0f, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_Y => new(0f, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y__ => new(0f, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __XX => new(0f, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XY => new(0f, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __X_ => new(0f, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __YX => new(0f, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YY => new(0f, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __Y_ => new(0f, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ___X => new(0f, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___Y => new(0f, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ____ => new(0f, 0f, 0f, 0f);

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public F3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XX_ => new(X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XY_ => new(X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 X_X => new(X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 X_Y => new(X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 X__ => new(X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YX_ => new(Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YY_ => new(Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 Y_X => new(Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Y_Y => new(Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Y__ => new(Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 _XX => new(0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _XY => new(0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _X_ => new(0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 _YX => new(0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _YY => new(0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _Y_ => new(0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 __X => new(0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 __Y => new(0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ___ => new(0f, 0f, 0f);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public F2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 X_ => new(X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 Y_ => new(Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F2 _X => new(0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 _Y => new(0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 __ => new(0f, 0f);

#endregion

}

partial struct Float3
{

#region Four

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXX => new(X, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXY => new(X, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXZ => new(X, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXX_ => new(X, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYX => new(X, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYY => new(X, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYZ => new(X, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXY_ => new(X, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZX => new(X, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZY => new(X, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZZ => new(X, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZ_ => new(X, X, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_X => new(X, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_Y => new(X, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_Z => new(X, X, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX__ => new(X, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXX => new(X, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXY => new(X, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXZ => new(X, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYX_ => new(X, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYX => new(X, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYY => new(X, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYZ => new(X, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYY_ => new(X, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZX => new(X, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZY => new(X, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZZ => new(X, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZ_ => new(X, Y, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_X => new(X, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_Y => new(X, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_Z => new(X, Y, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY__ => new(X, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXX => new(X, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXY => new(X, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXZ => new(X, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZX_ => new(X, Z, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYX => new(X, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYY => new(X, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYZ => new(X, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZY_ => new(X, Z, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZX => new(X, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZY => new(X, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZZ => new(X, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZ_ => new(X, Z, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_X => new(X, Z, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_Y => new(X, Z, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_Z => new(X, Z, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ__ => new(X, Z, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XX => new(X, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XY => new(X, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XZ => new(X, 0f, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_X_ => new(X, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YX => new(X, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YY => new(X, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YZ => new(X, 0f, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_Y_ => new(X, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZX => new(X, 0f, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZY => new(X, 0f, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZZ => new(X, 0f, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_Z_ => new(X, 0f, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X__X => new(X, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__Y => new(X, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__Z => new(X, 0f, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X___ => new(X, 0f, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXX => new(Y, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXY => new(Y, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXZ => new(Y, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXX_ => new(Y, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYX => new(Y, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYY => new(Y, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYZ => new(Y, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXY_ => new(Y, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZX => new(Y, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZY => new(Y, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZZ => new(Y, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZ_ => new(Y, X, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_X => new(Y, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_Y => new(Y, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_Z => new(Y, X, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX__ => new(Y, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXX => new(Y, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXY => new(Y, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXZ => new(Y, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYX_ => new(Y, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYX => new(Y, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYY => new(Y, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYZ => new(Y, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYY_ => new(Y, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZX => new(Y, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZY => new(Y, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZZ => new(Y, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZ_ => new(Y, Y, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_X => new(Y, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_Y => new(Y, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_Z => new(Y, Y, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY__ => new(Y, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXX => new(Y, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXY => new(Y, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXZ => new(Y, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZX_ => new(Y, Z, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYX => new(Y, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYY => new(Y, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYZ => new(Y, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZY_ => new(Y, Z, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZX => new(Y, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZY => new(Y, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZZ => new(Y, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZ_ => new(Y, Z, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_X => new(Y, Z, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_Y => new(Y, Z, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_Z => new(Y, Z, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ__ => new(Y, Z, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XX => new(Y, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XY => new(Y, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XZ => new(Y, 0f, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_X_ => new(Y, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YX => new(Y, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YY => new(Y, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YZ => new(Y, 0f, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_Y_ => new(Y, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZX => new(Y, 0f, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZY => new(Y, 0f, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZZ => new(Y, 0f, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_Z_ => new(Y, 0f, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__X => new(Y, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__Y => new(Y, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__Z => new(Y, 0f, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y___ => new(Y, 0f, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXX => new(Z, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXY => new(Z, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXZ => new(Z, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXX_ => new(Z, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYX => new(Z, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYY => new(Z, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYZ => new(Z, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXY_ => new(Z, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZX => new(Z, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZY => new(Z, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZZ => new(Z, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZ_ => new(Z, X, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_X => new(Z, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_Y => new(Z, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_Z => new(Z, X, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX__ => new(Z, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXX => new(Z, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXY => new(Z, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXZ => new(Z, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYX_ => new(Z, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYX => new(Z, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYY => new(Z, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYZ => new(Z, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYY_ => new(Z, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZX => new(Z, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZY => new(Z, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZZ => new(Z, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZ_ => new(Z, Y, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_X => new(Z, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_Y => new(Z, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_Z => new(Z, Y, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY__ => new(Z, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXX => new(Z, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXY => new(Z, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXZ => new(Z, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZX_ => new(Z, Z, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYX => new(Z, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYY => new(Z, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYZ => new(Z, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZY_ => new(Z, Z, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZX => new(Z, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZY => new(Z, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZZ => new(Z, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZ_ => new(Z, Z, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_X => new(Z, Z, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_Y => new(Z, Z, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_Z => new(Z, Z, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ__ => new(Z, Z, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XX => new(Z, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XY => new(Z, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XZ => new(Z, 0f, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_X_ => new(Z, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YX => new(Z, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YY => new(Z, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YZ => new(Z, 0f, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_Y_ => new(Z, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZX => new(Z, 0f, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZY => new(Z, 0f, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZZ => new(Z, 0f, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_Z_ => new(Z, 0f, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__X => new(Z, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__Y => new(Z, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__Z => new(Z, 0f, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z___ => new(Z, 0f, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXX => new(0f, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXY => new(0f, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXZ => new(0f, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XX_ => new(0f, X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYX => new(0f, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYY => new(0f, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYZ => new(0f, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XY_ => new(0f, X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZX => new(0f, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZY => new(0f, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZZ => new(0f, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZ_ => new(0f, X, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_X => new(0f, X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_Y => new(0f, X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_Z => new(0f, X, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X__ => new(0f, X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXX => new(0f, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXY => new(0f, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXZ => new(0f, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YX_ => new(0f, Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYX => new(0f, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYY => new(0f, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYZ => new(0f, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YY_ => new(0f, Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZX => new(0f, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZY => new(0f, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZZ => new(0f, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZ_ => new(0f, Y, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_X => new(0f, Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_Y => new(0f, Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_Z => new(0f, Y, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y__ => new(0f, Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXX => new(0f, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXY => new(0f, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXZ => new(0f, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZX_ => new(0f, Z, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYX => new(0f, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYY => new(0f, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYZ => new(0f, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZY_ => new(0f, Z, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZX => new(0f, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZY => new(0f, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZZ => new(0f, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZ_ => new(0f, Z, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_X => new(0f, Z, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_Y => new(0f, Z, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_Z => new(0f, Z, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z__ => new(0f, Z, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __XX => new(0f, 0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XY => new(0f, 0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XZ => new(0f, 0f, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __X_ => new(0f, 0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __YX => new(0f, 0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YY => new(0f, 0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YZ => new(0f, 0f, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __Y_ => new(0f, 0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZX => new(0f, 0f, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZY => new(0f, 0f, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZZ => new(0f, 0f, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __Z_ => new(0f, 0f, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ___X => new(0f, 0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___Y => new(0f, 0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___Z => new(0f, 0f, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ____ => new(0f, 0f, 0f, 0f);

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public F3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXZ => new(X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XX_ => new(X, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYZ => new(X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XY_ => new(X, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XZX => new(X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZY => new(X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZZ => new(X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZ_ => new(X, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 X_X => new(X, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 X_Y => new(X, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 X_Z => new(X, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 X__ => new(X, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXZ => new(Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YX_ => new(Y, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYZ => new(Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YY_ => new(Y, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YZX => new(Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZY => new(Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZZ => new(Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZ_ => new(Y, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 Y_X => new(Y, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Y_Y => new(Y, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Y_Z => new(Y, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Y__ => new(Y, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXX => new(Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXY => new(Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXZ => new(Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZX_ => new(Z, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYX => new(Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYY => new(Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYZ => new(Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZY_ => new(Z, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZX => new(Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZY => new(Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZZ => new(Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZ_ => new(Z, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 Z_X => new(Z, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Z_Y => new(Z, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Z_Z => new(Z, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 Z__ => new(Z, 0f, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 _XX => new(0f, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _XY => new(0f, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _XZ => new(0f, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _X_ => new(0f, X, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 _YX => new(0f, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _YY => new(0f, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _YZ => new(0f, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _Y_ => new(0f, Y, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 _ZX => new(0f, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _ZY => new(0f, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _ZZ => new(0f, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 _Z_ => new(0f, Z, 0f);

	[EB(EBS.Never), DB(DBS.Never)] public F3 __X => new(0f, 0f, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 __Y => new(0f, 0f, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 __Z => new(0f, 0f, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ___ => new(0f, 0f, 0f);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public F2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XZ => new(X, Z);

	[EB(EBS.Never), DB(DBS.Never)] public F2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YZ => new(Y, Z);

	[EB(EBS.Never), DB(DBS.Never)] public F2 ZX => new(Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 ZY => new(Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 ZZ => new(Z, Z);

#endregion

}

partial struct Float4
{

#region Four

	// ReSharper disable ShiftExpressionZeroLeftOperand

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	F4 Shuffle(byte shuffle) => new(Sse.Shuffle(v, v, shuffle));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	F4 Blend(byte blend) => new(Sse41.Blend(v, Vector128<float>.Zero, blend)); //OPTIMIZE: we can use _mm_insert_ps here to save one instruction

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	F4 ShuffleBlend(byte shuffle, byte blend) => new(Sse41.Blend(Sse.Shuffle(v, v, shuffle), Vector128<float>.Zero, blend));

	//Automatically generated by SwizzledGenerator.cs, decent SIMD quality for most cases.
	//Not optimal when there are two empties involved, can be improved with single shuffle.

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXX => Shuffle((0 << 0) | (0 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXY => Shuffle((0 << 0) | (0 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXZ => Shuffle((0 << 0) | (0 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXXW => Shuffle((0 << 0) | (0 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXX_ => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYX => Shuffle((0 << 0) | (0 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYY => Shuffle((0 << 0) | (0 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYZ => Shuffle((0 << 0) | (0 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXYW => Shuffle((0 << 0) | (0 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXY_ => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZX => Shuffle((0 << 0) | (0 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZY => Shuffle((0 << 0) | (0 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZZ => Shuffle((0 << 0) | (0 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZW => Shuffle((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXZ_ => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XXWX => Shuffle((0 << 0) | (0 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXWY => Shuffle((0 << 0) | (0 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXWZ => Shuffle((0 << 0) | (0 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXWW => Shuffle((0 << 0) | (0 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XXW_ => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_X => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_Y => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_Z => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX_W => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XX__ => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXX => Shuffle((0 << 0) | (1 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXY => Shuffle((0 << 0) | (1 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXZ => Shuffle((0 << 0) | (1 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYXW => Shuffle((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYX_ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYX => Shuffle((0 << 0) | (1 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYY => Shuffle((0 << 0) | (1 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYZ => Shuffle((0 << 0) | (1 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYYW => Shuffle((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYY_ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZX => Shuffle((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZY => Shuffle((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZZ => Shuffle((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZW => Shuffle((0 << 0) | (1 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYZ_ => Blend(0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XYWX => Shuffle((0 << 0) | (1 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYWY => Shuffle((0 << 0) | (1 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYWZ => Shuffle((0 << 0) | (1 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYWW => Shuffle((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XYW_ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_X => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_Y => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_Z => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY_W => Blend(0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XY__ => Blend(0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXX => Shuffle((0 << 0) | (2 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXY => Shuffle((0 << 0) | (2 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXZ => Shuffle((0 << 0) | (2 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZXW => Shuffle((0 << 0) | (2 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZX_ => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYX => Shuffle((0 << 0) | (2 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYY => Shuffle((0 << 0) | (2 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYZ => Shuffle((0 << 0) | (2 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZYW => Shuffle((0 << 0) | (2 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZY_ => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZX => Shuffle((0 << 0) | (2 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZY => Shuffle((0 << 0) | (2 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZZ => Shuffle((0 << 0) | (2 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZW => Shuffle((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZZ_ => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZWX => Shuffle((0 << 0) | (2 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZWY => Shuffle((0 << 0) | (2 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZWZ => Shuffle((0 << 0) | (2 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZWW => Shuffle((0 << 0) | (2 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZW_ => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_X => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_Y => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_Z => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ_W => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XZ__ => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XWXX => Shuffle((0 << 0) | (3 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWXY => Shuffle((0 << 0) | (3 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWXZ => Shuffle((0 << 0) | (3 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWXW => Shuffle((0 << 0) | (3 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWX_ => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XWYX => Shuffle((0 << 0) | (3 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWYY => Shuffle((0 << 0) | (3 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWYZ => Shuffle((0 << 0) | (3 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWYW => Shuffle((0 << 0) | (3 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWY_ => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XWZX => Shuffle((0 << 0) | (3 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWZY => Shuffle((0 << 0) | (3 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWZZ => Shuffle((0 << 0) | (3 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWZW => Shuffle((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWZ_ => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XWWX => Shuffle((0 << 0) | (3 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWWY => Shuffle((0 << 0) | (3 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWWZ => Shuffle((0 << 0) | (3 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWWW => Shuffle((0 << 0) | (3 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 XWW_ => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 XW_X => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XW_Y => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XW_Z => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XW_W => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 XW__ => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XX => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XY => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XZ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_XW => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_X_ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YX => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YY => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YZ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_YW => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_Y_ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZX => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZY => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZZ => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_ZW => Blend(0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_Z_ => Blend(0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X_WX => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_WY => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_WZ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_WW => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X_W_ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 X__X => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__Y => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__Z => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X__W => Blend(0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 X___ => Blend(0b1110);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXX => Shuffle((1 << 0) | (0 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXY => Shuffle((1 << 0) | (0 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXZ => Shuffle((1 << 0) | (0 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXXW => Shuffle((1 << 0) | (0 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXX_ => ShuffleBlend((1 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYX => Shuffle((1 << 0) | (0 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYY => Shuffle((1 << 0) | (0 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYZ => Shuffle((1 << 0) | (0 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXYW => Shuffle((1 << 0) | (0 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXY_ => ShuffleBlend((1 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZX => Shuffle((1 << 0) | (0 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZY => Shuffle((1 << 0) | (0 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZZ => Shuffle((1 << 0) | (0 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZW => Shuffle((1 << 0) | (0 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXZ_ => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YXWX => Shuffle((1 << 0) | (0 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXWY => Shuffle((1 << 0) | (0 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXWZ => Shuffle((1 << 0) | (0 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXWW => Shuffle((1 << 0) | (0 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YXW_ => ShuffleBlend((1 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_X => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_Y => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_Z => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX_W => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YX__ => ShuffleBlend((1 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXX => Shuffle((1 << 0) | (1 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXY => Shuffle((1 << 0) | (1 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXZ => Shuffle((1 << 0) | (1 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYXW => Shuffle((1 << 0) | (1 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYX_ => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYX => Shuffle((1 << 0) | (1 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYY => Shuffle((1 << 0) | (1 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYZ => Shuffle((1 << 0) | (1 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYYW => Shuffle((1 << 0) | (1 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYY_ => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZX => Shuffle((1 << 0) | (1 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZY => Shuffle((1 << 0) | (1 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZZ => Shuffle((1 << 0) | (1 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZW => Shuffle((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYZ_ => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YYWX => Shuffle((1 << 0) | (1 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYWY => Shuffle((1 << 0) | (1 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYWZ => Shuffle((1 << 0) | (1 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYWW => Shuffle((1 << 0) | (1 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YYW_ => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_X => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_Y => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_Z => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY_W => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YY__ => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXX => Shuffle((1 << 0) | (2 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXY => Shuffle((1 << 0) | (2 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXZ => Shuffle((1 << 0) | (2 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZXW => Shuffle((1 << 0) | (2 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZX_ => ShuffleBlend((1 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYX => Shuffle((1 << 0) | (2 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYY => Shuffle((1 << 0) | (2 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYZ => Shuffle((1 << 0) | (2 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZYW => Shuffle((1 << 0) | (2 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZY_ => ShuffleBlend((1 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZX => Shuffle((1 << 0) | (2 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZY => Shuffle((1 << 0) | (2 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZZ => Shuffle((1 << 0) | (2 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZW => Shuffle((1 << 0) | (2 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZZ_ => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZWX => Shuffle((1 << 0) | (2 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZWY => Shuffle((1 << 0) | (2 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZWZ => Shuffle((1 << 0) | (2 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZWW => Shuffle((1 << 0) | (2 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZW_ => ShuffleBlend((1 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_X => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_Y => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_Z => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ_W => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YZ__ => ShuffleBlend((1 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YWXX => Shuffle((1 << 0) | (3 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWXY => Shuffle((1 << 0) | (3 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWXZ => Shuffle((1 << 0) | (3 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWXW => Shuffle((1 << 0) | (3 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWX_ => ShuffleBlend((1 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YWYX => Shuffle((1 << 0) | (3 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWYY => Shuffle((1 << 0) | (3 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWYZ => Shuffle((1 << 0) | (3 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWYW => Shuffle((1 << 0) | (3 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWY_ => ShuffleBlend((1 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YWZX => Shuffle((1 << 0) | (3 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWZY => Shuffle((1 << 0) | (3 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWZZ => Shuffle((1 << 0) | (3 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWZW => Shuffle((1 << 0) | (3 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWZ_ => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YWWX => Shuffle((1 << 0) | (3 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWWY => Shuffle((1 << 0) | (3 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWWZ => Shuffle((1 << 0) | (3 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWWW => Shuffle((1 << 0) | (3 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 YWW_ => ShuffleBlend((1 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 YW_X => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YW_Y => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YW_Z => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YW_W => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 YW__ => ShuffleBlend((1 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XX => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XY => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XZ => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_XW => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_X_ => ShuffleBlend((1 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YX => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YY => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YZ => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_YW => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_Y_ => ShuffleBlend((1 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZX => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZY => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZZ => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_ZW => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_Z_ => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_WX => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_WY => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_WZ => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_WW => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y_W_ => ShuffleBlend((1 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__X => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__Y => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__Z => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y__W => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Y___ => ShuffleBlend((1 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1110);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXX => Shuffle((2 << 0) | (0 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXY => Shuffle((2 << 0) | (0 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXZ => Shuffle((2 << 0) | (0 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXXW => Shuffle((2 << 0) | (0 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXX_ => ShuffleBlend((2 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYX => Shuffle((2 << 0) | (0 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYY => Shuffle((2 << 0) | (0 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYZ => Shuffle((2 << 0) | (0 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXYW => Shuffle((2 << 0) | (0 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXY_ => ShuffleBlend((2 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZX => Shuffle((2 << 0) | (0 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZY => Shuffle((2 << 0) | (0 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZZ => Shuffle((2 << 0) | (0 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZW => Shuffle((2 << 0) | (0 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXZ_ => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXWX => Shuffle((2 << 0) | (0 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXWY => Shuffle((2 << 0) | (0 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXWZ => Shuffle((2 << 0) | (0 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXWW => Shuffle((2 << 0) | (0 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZXW_ => ShuffleBlend((2 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_X => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_Y => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_Z => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX_W => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZX__ => ShuffleBlend((2 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXX => Shuffle((2 << 0) | (1 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXY => Shuffle((2 << 0) | (1 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXZ => Shuffle((2 << 0) | (1 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYXW => Shuffle((2 << 0) | (1 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYX_ => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYX => Shuffle((2 << 0) | (1 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYY => Shuffle((2 << 0) | (1 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYZ => Shuffle((2 << 0) | (1 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYYW => Shuffle((2 << 0) | (1 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYY_ => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZX => Shuffle((2 << 0) | (1 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZY => Shuffle((2 << 0) | (1 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZZ => Shuffle((2 << 0) | (1 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZW => Shuffle((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYZ_ => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYWX => Shuffle((2 << 0) | (1 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYWY => Shuffle((2 << 0) | (1 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYWZ => Shuffle((2 << 0) | (1 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYWW => Shuffle((2 << 0) | (1 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZYW_ => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_X => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_Y => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_Z => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY_W => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZY__ => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXX => Shuffle((2 << 0) | (2 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXY => Shuffle((2 << 0) | (2 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXZ => Shuffle((2 << 0) | (2 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZXW => Shuffle((2 << 0) | (2 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZX_ => ShuffleBlend((2 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYX => Shuffle((2 << 0) | (2 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYY => Shuffle((2 << 0) | (2 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYZ => Shuffle((2 << 0) | (2 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZYW => Shuffle((2 << 0) | (2 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZY_ => ShuffleBlend((2 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZX => Shuffle((2 << 0) | (2 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZY => Shuffle((2 << 0) | (2 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZZ => Shuffle((2 << 0) | (2 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZW => Shuffle((2 << 0) | (2 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZZ_ => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZWX => Shuffle((2 << 0) | (2 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZWY => Shuffle((2 << 0) | (2 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZWZ => Shuffle((2 << 0) | (2 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZWW => Shuffle((2 << 0) | (2 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZW_ => ShuffleBlend((2 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_X => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_Y => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_Z => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ_W => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZZ__ => ShuffleBlend((2 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWXX => Shuffle((2 << 0) | (3 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWXY => Shuffle((2 << 0) | (3 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWXZ => Shuffle((2 << 0) | (3 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWXW => Shuffle((2 << 0) | (3 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWX_ => ShuffleBlend((2 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWYX => Shuffle((2 << 0) | (3 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWYY => Shuffle((2 << 0) | (3 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWYZ => Shuffle((2 << 0) | (3 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWYW => Shuffle((2 << 0) | (3 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWY_ => ShuffleBlend((2 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWZX => Shuffle((2 << 0) | (3 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWZY => Shuffle((2 << 0) | (3 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWZZ => Shuffle((2 << 0) | (3 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWZW => Shuffle((2 << 0) | (3 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWZ_ => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWWX => Shuffle((2 << 0) | (3 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWWY => Shuffle((2 << 0) | (3 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWWZ => Shuffle((2 << 0) | (3 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWWW => Shuffle((2 << 0) | (3 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZWW_ => ShuffleBlend((2 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ZW_X => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZW_Y => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZW_Z => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZW_W => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ZW__ => ShuffleBlend((2 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XX => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XY => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XZ => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_XW => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_X_ => ShuffleBlend((2 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YX => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YY => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YZ => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_YW => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_Y_ => ShuffleBlend((2 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZX => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZY => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZZ => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_ZW => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_Z_ => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_WX => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_WY => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_WZ => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_WW => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z_W_ => ShuffleBlend((2 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__X => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__Y => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__Z => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z__W => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 Z___ => ShuffleBlend((2 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1110);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WXXX => Shuffle((3 << 0) | (0 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXXY => Shuffle((3 << 0) | (0 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXXZ => Shuffle((3 << 0) | (0 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXXW => Shuffle((3 << 0) | (0 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXX_ => ShuffleBlend((3 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WXYX => Shuffle((3 << 0) | (0 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXYY => Shuffle((3 << 0) | (0 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXYZ => Shuffle((3 << 0) | (0 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXYW => Shuffle((3 << 0) | (0 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXY_ => ShuffleBlend((3 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WXZX => Shuffle((3 << 0) | (0 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXZY => Shuffle((3 << 0) | (0 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXZZ => Shuffle((3 << 0) | (0 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXZW => Shuffle((3 << 0) | (0 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXZ_ => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WXWX => Shuffle((3 << 0) | (0 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXWY => Shuffle((3 << 0) | (0 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXWZ => Shuffle((3 << 0) | (0 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXWW => Shuffle((3 << 0) | (0 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WXW_ => ShuffleBlend((3 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WX_X => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WX_Y => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WX_Z => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WX_W => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WX__ => ShuffleBlend((3 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WYXX => Shuffle((3 << 0) | (1 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYXY => Shuffle((3 << 0) | (1 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYXZ => Shuffle((3 << 0) | (1 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYXW => Shuffle((3 << 0) | (1 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYX_ => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WYYX => Shuffle((3 << 0) | (1 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYYY => Shuffle((3 << 0) | (1 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYYZ => Shuffle((3 << 0) | (1 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYYW => Shuffle((3 << 0) | (1 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYY_ => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WYZX => Shuffle((3 << 0) | (1 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYZY => Shuffle((3 << 0) | (1 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYZZ => Shuffle((3 << 0) | (1 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYZW => Shuffle((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYZ_ => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WYWX => Shuffle((3 << 0) | (1 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYWY => Shuffle((3 << 0) | (1 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYWZ => Shuffle((3 << 0) | (1 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYWW => Shuffle((3 << 0) | (1 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WYW_ => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WY_X => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WY_Y => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WY_Z => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WY_W => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WY__ => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WZXX => Shuffle((3 << 0) | (2 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZXY => Shuffle((3 << 0) | (2 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZXZ => Shuffle((3 << 0) | (2 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZXW => Shuffle((3 << 0) | (2 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZX_ => ShuffleBlend((3 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WZYX => Shuffle((3 << 0) | (2 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZYY => Shuffle((3 << 0) | (2 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZYZ => Shuffle((3 << 0) | (2 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZYW => Shuffle((3 << 0) | (2 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZY_ => ShuffleBlend((3 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WZZX => Shuffle((3 << 0) | (2 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZZY => Shuffle((3 << 0) | (2 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZZZ => Shuffle((3 << 0) | (2 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZZW => Shuffle((3 << 0) | (2 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZZ_ => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WZWX => Shuffle((3 << 0) | (2 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZWY => Shuffle((3 << 0) | (2 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZWZ => Shuffle((3 << 0) | (2 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZWW => Shuffle((3 << 0) | (2 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZW_ => ShuffleBlend((3 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WZ_X => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZ_Y => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZ_Z => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZ_W => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WZ__ => ShuffleBlend((3 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WWXX => Shuffle((3 << 0) | (3 << 2) | (0 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWXY => Shuffle((3 << 0) | (3 << 2) | (0 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWXZ => Shuffle((3 << 0) | (3 << 2) | (0 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWXW => Shuffle((3 << 0) | (3 << 2) | (0 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWX_ => ShuffleBlend((3 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WWYX => Shuffle((3 << 0) | (3 << 2) | (1 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWYY => Shuffle((3 << 0) | (3 << 2) | (1 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWYZ => Shuffle((3 << 0) | (3 << 2) | (1 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWYW => Shuffle((3 << 0) | (3 << 2) | (1 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWY_ => ShuffleBlend((3 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WWZX => Shuffle((3 << 0) | (3 << 2) | (2 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWZY => Shuffle((3 << 0) | (3 << 2) | (2 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWZZ => Shuffle((3 << 0) | (3 << 2) | (2 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWZW => Shuffle((3 << 0) | (3 << 2) | (2 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWZ_ => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WWWX => Shuffle((3 << 0) | (3 << 2) | (3 << 4) | (0 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWWY => Shuffle((3 << 0) | (3 << 2) | (3 << 4) | (1 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWWZ => Shuffle((3 << 0) | (3 << 2) | (3 << 4) | (2 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWWW => Shuffle((3 << 0) | (3 << 2) | (3 << 4) | (3 << 6));
	[EB(EBS.Never), DB(DBS.Never)] public F4 WWW_ => ShuffleBlend((3 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b1000);

	[EB(EBS.Never), DB(DBS.Never)] public F4 WW_X => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WW_Y => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WW_Z => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WW_W => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0100);
	[EB(EBS.Never), DB(DBS.Never)] public F4 WW__ => ShuffleBlend((3 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1100);

	[EB(EBS.Never), DB(DBS.Never)] public F4 W_XX => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_XY => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_XZ => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_XW => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_X_ => ShuffleBlend((3 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 W_YX => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_YY => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_YZ => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_YW => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_Y_ => ShuffleBlend((3 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 W_ZX => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_ZY => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_ZZ => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_ZW => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_Z_ => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 W_WX => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_WY => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_WZ => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_WW => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0010);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W_W_ => ShuffleBlend((3 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1010);

	[EB(EBS.Never), DB(DBS.Never)] public F4 W__X => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W__Y => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W__Z => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W__W => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b0110);
	[EB(EBS.Never), DB(DBS.Never)] public F4 W___ => ShuffleBlend((3 << 0) | (1 << 2) | (2 << 4) | (3 << 6), 0b1110);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXX => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXY => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXZ => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XXW => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XX_ => ShuffleBlend((0 << 0) | (0 << 2) | (0 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYX => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYY => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYZ => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XYW => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XY_ => ShuffleBlend((0 << 0) | (0 << 2) | (1 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZX => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZY => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZZ => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZW => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XZ_ => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _XWX => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XWY => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XWZ => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XWW => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _XW_ => ShuffleBlend((0 << 0) | (0 << 2) | (3 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_X => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (0 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_Y => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (1 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_Z => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (2 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X_W => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _X__ => ShuffleBlend((0 << 0) | (0 << 2) | (2 << 4) | (3 << 6), 0b1101);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXX => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXY => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXZ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YXW => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YX_ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYX => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYY => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYZ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YYW => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YY_ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZX => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZY => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZZ => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZW => Blend(0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YZ_ => Blend(0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _YWX => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YWY => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YWZ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YWW => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _YW_ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_X => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_Y => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_Z => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y_W => Blend(0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Y__ => Blend(0b1101);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXX => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXY => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXZ => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZXW => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZX_ => ShuffleBlend((0 << 0) | (2 << 2) | (0 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYX => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYY => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYZ => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZYW => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZY_ => ShuffleBlend((0 << 0) | (2 << 2) | (1 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZX => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZY => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZZ => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZW => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZZ_ => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZWX => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZWY => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZWZ => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZWW => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _ZW_ => ShuffleBlend((0 << 0) | (2 << 2) | (3 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_X => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (0 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_Y => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (1 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_Z => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (2 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z_W => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _Z__ => ShuffleBlend((0 << 0) | (2 << 2) | (2 << 4) | (3 << 6), 0b1101);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _WXX => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WXY => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WXZ => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WXW => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WX_ => ShuffleBlend((0 << 0) | (3 << 2) | (0 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _WYX => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WYY => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WYZ => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WYW => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WY_ => ShuffleBlend((0 << 0) | (3 << 2) | (1 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _WZX => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WZY => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WZZ => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WZW => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WZ_ => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _WWX => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (0 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WWY => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (1 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WWZ => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (2 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WWW => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b0001);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _WW_ => ShuffleBlend((0 << 0) | (3 << 2) | (3 << 4) | (3 << 6), 0b1001);

	[EB(EBS.Never), DB(DBS.Never)] public F4 _W_X => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (0 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _W_Y => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (1 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _W_Z => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (2 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _W_W => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b0101);
	[EB(EBS.Never), DB(DBS.Never)] public F4 _W__ => ShuffleBlend((0 << 0) | (3 << 2) | (2 << 4) | (3 << 6), 0b1101);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __XX => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (0 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XY => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (1 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XZ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (2 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __XW => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __X_ => ShuffleBlend((0 << 0) | (1 << 2) | (0 << 4) | (3 << 6), 0b1011);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __YX => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (0 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YY => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (1 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YZ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (2 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __YW => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __Y_ => ShuffleBlend((0 << 0) | (1 << 2) | (1 << 4) | (3 << 6), 0b1011);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZX => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZY => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZZ => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __ZW => Blend(0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __Z_ => Blend(0b1011);

	[EB(EBS.Never), DB(DBS.Never)] public F4 __WX => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (0 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __WY => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (1 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __WZ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (2 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __WW => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b0011);
	[EB(EBS.Never), DB(DBS.Never)] public F4 __W_ => ShuffleBlend((0 << 0) | (1 << 2) | (3 << 4) | (3 << 6), 0b1011);

	[EB(EBS.Never), DB(DBS.Never)] public F4 ___X => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (0 << 6), 0b0111);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___Y => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (1 << 6), 0b0111);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___Z => ShuffleBlend((0 << 0) | (1 << 2) | (2 << 4) | (2 << 6), 0b0111);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ___W => Blend(0b0111);
	[EB(EBS.Never), DB(DBS.Never)] public F4 ____ => Blend(0b1111);

	// ReSharper restore ShiftExpressionZeroLeftOperand

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public F3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXZ => new(X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XXW => new(X, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYZ => new(X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XYW => new(X, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XZX => new(X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZY => new(X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZZ => new(X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XZW => new(X, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 XWX => new(X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XWY => new(X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XWZ => new(X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 XWW => new(X, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXZ => new(Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YXW => new(Y, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYZ => new(Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YYW => new(Y, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YZX => new(Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZY => new(Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZZ => new(Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YZW => new(Y, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 YWX => new(Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YWY => new(Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YWZ => new(Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 YWW => new(Y, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXX => new(Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXY => new(Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXZ => new(Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZXW => new(Z, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYX => new(Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYY => new(Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYZ => new(Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZYW => new(Z, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZX => new(Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZY => new(Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZZ => new(Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZZW => new(Z, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 ZWX => new(Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZWY => new(Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZWZ => new(Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 ZWW => new(Z, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 WXX => new(W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WXY => new(W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WXZ => new(W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WXW => new(W, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 WYX => new(W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WYY => new(W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WYZ => new(W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WYW => new(W, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 WZX => new(W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WZY => new(W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WZZ => new(W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WZW => new(W, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public F3 WWX => new(W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WWY => new(W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WWZ => new(W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F3 WWW => new(W, W, W);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public F2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XZ => new(X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F2 XW => new(X, W);

	[EB(EBS.Never), DB(DBS.Never)] public F2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YZ => new(Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F2 YW => new(Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public F2 ZX => new(Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 ZY => new(Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 ZZ => new(Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F2 ZW => new(Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public F2 WX => new(W, X);
	[EB(EBS.Never), DB(DBS.Never)] public F2 WY => new(W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public F2 WZ => new(W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public F2 WW => new(W, W);

#endregion

}

partial struct Int2
{

#region Four

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXX => new(X, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXY => new(X, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXX_ => new(X, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYX => new(X, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYY => new(X, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXY_ => new(X, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_X => new(X, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_Y => new(X, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX__ => new(X, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXX => new(X, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXY => new(X, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYX_ => new(X, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYX => new(X, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYY => new(X, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYY_ => new(X, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_X => new(X, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_Y => new(X, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY__ => new(X, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XX => new(X, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XY => new(X, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_X_ => new(X, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YX => new(X, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YY => new(X, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_Y_ => new(X, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X__X => new(X, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__Y => new(X, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X___ => new(X, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXX => new(Y, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXY => new(Y, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXX_ => new(Y, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYX => new(Y, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYY => new(Y, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXY_ => new(Y, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_X => new(Y, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_Y => new(Y, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX__ => new(Y, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXX => new(Y, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXY => new(Y, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYX_ => new(Y, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYX => new(Y, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYY => new(Y, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYY_ => new(Y, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_X => new(Y, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_Y => new(Y, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY__ => new(Y, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XX => new(Y, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XY => new(Y, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_X_ => new(Y, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YX => new(Y, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YY => new(Y, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_Y_ => new(Y, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__X => new(Y, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__Y => new(Y, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y___ => new(Y, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXX => new(0, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXY => new(0, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XX_ => new(0, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYX => new(0, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYY => new(0, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XY_ => new(0, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_X => new(0, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_Y => new(0, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X__ => new(0, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXX => new(0, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXY => new(0, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YX_ => new(0, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYX => new(0, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYY => new(0, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YY_ => new(0, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_X => new(0, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_Y => new(0, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y__ => new(0, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __XX => new(0, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XY => new(0, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __X_ => new(0, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __YX => new(0, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YY => new(0, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __Y_ => new(0, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ___X => new(0, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___Y => new(0, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ____ => new(0, 0, 0, 0);

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public I3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XX_ => new(X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XY_ => new(X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 X_X => new(X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 X_Y => new(X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 X__ => new(X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YX_ => new(Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YY_ => new(Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 Y_X => new(Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Y_Y => new(Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Y__ => new(Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 _XX => new(0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _XY => new(0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _X_ => new(0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 _YX => new(0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _YY => new(0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _Y_ => new(0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 __X => new(0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 __Y => new(0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ___ => new(0, 0, 0);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public I2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 X_ => new(X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 Y_ => new(Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I2 _X => new(0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 _Y => new(0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 __ => new(0, 0);

#endregion

}

partial struct Int3
{

#region Four

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXX => new(X, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXY => new(X, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXZ => new(X, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXX_ => new(X, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYX => new(X, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYY => new(X, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYZ => new(X, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXY_ => new(X, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZX => new(X, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZY => new(X, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZZ => new(X, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZ_ => new(X, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_X => new(X, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_Y => new(X, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_Z => new(X, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX__ => new(X, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXX => new(X, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXY => new(X, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXZ => new(X, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYX_ => new(X, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYX => new(X, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYY => new(X, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYZ => new(X, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYY_ => new(X, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZX => new(X, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZY => new(X, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZZ => new(X, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZ_ => new(X, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_X => new(X, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_Y => new(X, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_Z => new(X, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY__ => new(X, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXX => new(X, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXY => new(X, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXZ => new(X, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZX_ => new(X, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYX => new(X, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYY => new(X, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYZ => new(X, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZY_ => new(X, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZX => new(X, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZY => new(X, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZZ => new(X, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZ_ => new(X, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_X => new(X, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_Y => new(X, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_Z => new(X, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ__ => new(X, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XX => new(X, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XY => new(X, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XZ => new(X, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_X_ => new(X, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YX => new(X, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YY => new(X, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YZ => new(X, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_Y_ => new(X, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZX => new(X, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZY => new(X, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZZ => new(X, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_Z_ => new(X, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X__X => new(X, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__Y => new(X, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__Z => new(X, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X___ => new(X, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXX => new(Y, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXY => new(Y, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXZ => new(Y, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXX_ => new(Y, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYX => new(Y, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYY => new(Y, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYZ => new(Y, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXY_ => new(Y, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZX => new(Y, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZY => new(Y, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZZ => new(Y, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZ_ => new(Y, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_X => new(Y, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_Y => new(Y, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_Z => new(Y, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX__ => new(Y, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXX => new(Y, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXY => new(Y, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXZ => new(Y, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYX_ => new(Y, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYX => new(Y, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYY => new(Y, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYZ => new(Y, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYY_ => new(Y, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZX => new(Y, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZY => new(Y, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZZ => new(Y, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZ_ => new(Y, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_X => new(Y, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_Y => new(Y, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_Z => new(Y, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY__ => new(Y, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXX => new(Y, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXY => new(Y, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXZ => new(Y, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZX_ => new(Y, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYX => new(Y, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYY => new(Y, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYZ => new(Y, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZY_ => new(Y, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZX => new(Y, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZY => new(Y, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZZ => new(Y, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZ_ => new(Y, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_X => new(Y, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_Y => new(Y, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_Z => new(Y, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ__ => new(Y, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XX => new(Y, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XY => new(Y, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XZ => new(Y, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_X_ => new(Y, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YX => new(Y, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YY => new(Y, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YZ => new(Y, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_Y_ => new(Y, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZX => new(Y, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZY => new(Y, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZZ => new(Y, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_Z_ => new(Y, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__X => new(Y, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__Y => new(Y, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__Z => new(Y, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y___ => new(Y, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXX => new(Z, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXY => new(Z, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXZ => new(Z, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXX_ => new(Z, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYX => new(Z, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYY => new(Z, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYZ => new(Z, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXY_ => new(Z, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZX => new(Z, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZY => new(Z, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZZ => new(Z, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZ_ => new(Z, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_X => new(Z, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_Y => new(Z, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_Z => new(Z, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX__ => new(Z, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXX => new(Z, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXY => new(Z, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXZ => new(Z, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYX_ => new(Z, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYX => new(Z, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYY => new(Z, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYZ => new(Z, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYY_ => new(Z, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZX => new(Z, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZY => new(Z, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZZ => new(Z, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZ_ => new(Z, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_X => new(Z, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_Y => new(Z, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_Z => new(Z, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY__ => new(Z, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXX => new(Z, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXY => new(Z, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXZ => new(Z, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZX_ => new(Z, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYX => new(Z, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYY => new(Z, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYZ => new(Z, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZY_ => new(Z, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZX => new(Z, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZY => new(Z, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZZ => new(Z, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZ_ => new(Z, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_X => new(Z, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_Y => new(Z, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_Z => new(Z, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ__ => new(Z, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XX => new(Z, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XY => new(Z, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XZ => new(Z, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_X_ => new(Z, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YX => new(Z, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YY => new(Z, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YZ => new(Z, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_Y_ => new(Z, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZX => new(Z, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZY => new(Z, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZZ => new(Z, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_Z_ => new(Z, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__X => new(Z, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__Y => new(Z, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__Z => new(Z, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z___ => new(Z, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXX => new(0, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXY => new(0, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXZ => new(0, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XX_ => new(0, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYX => new(0, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYY => new(0, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYZ => new(0, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XY_ => new(0, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZX => new(0, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZY => new(0, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZZ => new(0, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZ_ => new(0, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_X => new(0, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_Y => new(0, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_Z => new(0, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X__ => new(0, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXX => new(0, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXY => new(0, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXZ => new(0, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YX_ => new(0, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYX => new(0, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYY => new(0, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYZ => new(0, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YY_ => new(0, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZX => new(0, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZY => new(0, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZZ => new(0, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZ_ => new(0, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_X => new(0, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_Y => new(0, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_Z => new(0, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y__ => new(0, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXX => new(0, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXY => new(0, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXZ => new(0, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZX_ => new(0, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYX => new(0, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYY => new(0, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYZ => new(0, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZY_ => new(0, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZX => new(0, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZY => new(0, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZZ => new(0, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZ_ => new(0, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_X => new(0, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_Y => new(0, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_Z => new(0, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z__ => new(0, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __XX => new(0, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XY => new(0, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XZ => new(0, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __X_ => new(0, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __YX => new(0, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YY => new(0, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YZ => new(0, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __Y_ => new(0, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZX => new(0, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZY => new(0, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZZ => new(0, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __Z_ => new(0, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ___X => new(0, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___Y => new(0, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___Z => new(0, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ____ => new(0, 0, 0, 0);

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public I3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXZ => new(X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XX_ => new(X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYZ => new(X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XY_ => new(X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XZX => new(X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZY => new(X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZZ => new(X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZ_ => new(X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 X_X => new(X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 X_Y => new(X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 X_Z => new(X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 X__ => new(X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXZ => new(Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YX_ => new(Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYZ => new(Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YY_ => new(Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YZX => new(Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZY => new(Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZZ => new(Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZ_ => new(Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 Y_X => new(Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Y_Y => new(Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Y_Z => new(Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Y__ => new(Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXX => new(Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXY => new(Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXZ => new(Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZX_ => new(Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYX => new(Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYY => new(Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYZ => new(Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZY_ => new(Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZX => new(Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZY => new(Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZZ => new(Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZ_ => new(Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 Z_X => new(Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Z_Y => new(Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Z_Z => new(Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 Z__ => new(Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 _XX => new(0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _XY => new(0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _XZ => new(0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _X_ => new(0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 _YX => new(0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _YY => new(0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _YZ => new(0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _Y_ => new(0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 _ZX => new(0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _ZY => new(0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _ZZ => new(0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 _Z_ => new(0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I3 __X => new(0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 __Y => new(0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 __Z => new(0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ___ => new(0, 0, 0);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public I2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XZ => new(X, Z);

	[EB(EBS.Never), DB(DBS.Never)] public I2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YZ => new(Y, Z);

	[EB(EBS.Never), DB(DBS.Never)] public I2 ZX => new(Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 ZY => new(Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 ZZ => new(Z, Z);

#endregion

}

partial struct Int4
{

#region Four

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXX => new(X, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXY => new(X, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXZ => new(X, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXXW => new(X, X, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXX_ => new(X, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYX => new(X, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYY => new(X, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYZ => new(X, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXYW => new(X, X, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXY_ => new(X, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZX => new(X, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZY => new(X, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZZ => new(X, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZW => new(X, X, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXZ_ => new(X, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XXWX => new(X, X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXWY => new(X, X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXWZ => new(X, X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXWW => new(X, X, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XXW_ => new(X, X, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_X => new(X, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_Y => new(X, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_Z => new(X, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX_W => new(X, X, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XX__ => new(X, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXX => new(X, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXY => new(X, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXZ => new(X, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYXW => new(X, Y, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYX_ => new(X, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYX => new(X, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYY => new(X, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYZ => new(X, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYYW => new(X, Y, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYY_ => new(X, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZX => new(X, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZY => new(X, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZZ => new(X, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZW => new(X, Y, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYZ_ => new(X, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XYWX => new(X, Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYWY => new(X, Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYWZ => new(X, Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYWW => new(X, Y, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XYW_ => new(X, Y, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_X => new(X, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_Y => new(X, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_Z => new(X, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY_W => new(X, Y, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XY__ => new(X, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXX => new(X, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXY => new(X, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXZ => new(X, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZXW => new(X, Z, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZX_ => new(X, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYX => new(X, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYY => new(X, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYZ => new(X, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZYW => new(X, Z, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZY_ => new(X, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZX => new(X, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZY => new(X, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZZ => new(X, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZW => new(X, Z, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZZ_ => new(X, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZWX => new(X, Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZWY => new(X, Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZWZ => new(X, Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZWW => new(X, Z, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZW_ => new(X, Z, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_X => new(X, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_Y => new(X, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_Z => new(X, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ_W => new(X, Z, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XZ__ => new(X, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XWXX => new(X, W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWXY => new(X, W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWXZ => new(X, W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWXW => new(X, W, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWX_ => new(X, W, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XWYX => new(X, W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWYY => new(X, W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWYZ => new(X, W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWYW => new(X, W, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWY_ => new(X, W, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XWZX => new(X, W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWZY => new(X, W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWZZ => new(X, W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWZW => new(X, W, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWZ_ => new(X, W, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XWWX => new(X, W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWWY => new(X, W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWWZ => new(X, W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWWW => new(X, W, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XWW_ => new(X, W, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 XW_X => new(X, W, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XW_Y => new(X, W, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XW_Z => new(X, W, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XW_W => new(X, W, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 XW__ => new(X, W, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XX => new(X, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XY => new(X, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XZ => new(X, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_XW => new(X, 0, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_X_ => new(X, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YX => new(X, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YY => new(X, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YZ => new(X, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_YW => new(X, 0, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_Y_ => new(X, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZX => new(X, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZY => new(X, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZZ => new(X, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_ZW => new(X, 0, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_Z_ => new(X, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X_WX => new(X, 0, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_WY => new(X, 0, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_WZ => new(X, 0, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_WW => new(X, 0, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X_W_ => new(X, 0, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 X__X => new(X, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__Y => new(X, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__Z => new(X, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X__W => new(X, 0, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 X___ => new(X, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXX => new(Y, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXY => new(Y, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXZ => new(Y, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXXW => new(Y, X, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXX_ => new(Y, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYX => new(Y, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYY => new(Y, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYZ => new(Y, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXYW => new(Y, X, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXY_ => new(Y, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZX => new(Y, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZY => new(Y, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZZ => new(Y, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZW => new(Y, X, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXZ_ => new(Y, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YXWX => new(Y, X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXWY => new(Y, X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXWZ => new(Y, X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXWW => new(Y, X, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YXW_ => new(Y, X, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_X => new(Y, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_Y => new(Y, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_Z => new(Y, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX_W => new(Y, X, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YX__ => new(Y, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXX => new(Y, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXY => new(Y, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXZ => new(Y, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYXW => new(Y, Y, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYX_ => new(Y, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYX => new(Y, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYY => new(Y, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYZ => new(Y, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYYW => new(Y, Y, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYY_ => new(Y, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZX => new(Y, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZY => new(Y, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZZ => new(Y, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZW => new(Y, Y, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYZ_ => new(Y, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YYWX => new(Y, Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYWY => new(Y, Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYWZ => new(Y, Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYWW => new(Y, Y, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YYW_ => new(Y, Y, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_X => new(Y, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_Y => new(Y, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_Z => new(Y, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY_W => new(Y, Y, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YY__ => new(Y, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXX => new(Y, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXY => new(Y, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXZ => new(Y, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZXW => new(Y, Z, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZX_ => new(Y, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYX => new(Y, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYY => new(Y, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYZ => new(Y, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZYW => new(Y, Z, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZY_ => new(Y, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZX => new(Y, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZY => new(Y, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZZ => new(Y, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZW => new(Y, Z, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZZ_ => new(Y, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZWX => new(Y, Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZWY => new(Y, Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZWZ => new(Y, Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZWW => new(Y, Z, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZW_ => new(Y, Z, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_X => new(Y, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_Y => new(Y, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_Z => new(Y, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ_W => new(Y, Z, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YZ__ => new(Y, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YWXX => new(Y, W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWXY => new(Y, W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWXZ => new(Y, W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWXW => new(Y, W, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWX_ => new(Y, W, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YWYX => new(Y, W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWYY => new(Y, W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWYZ => new(Y, W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWYW => new(Y, W, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWY_ => new(Y, W, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YWZX => new(Y, W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWZY => new(Y, W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWZZ => new(Y, W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWZW => new(Y, W, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWZ_ => new(Y, W, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YWWX => new(Y, W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWWY => new(Y, W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWWZ => new(Y, W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWWW => new(Y, W, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YWW_ => new(Y, W, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 YW_X => new(Y, W, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YW_Y => new(Y, W, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YW_Z => new(Y, W, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YW_W => new(Y, W, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 YW__ => new(Y, W, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XX => new(Y, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XY => new(Y, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XZ => new(Y, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_XW => new(Y, 0, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_X_ => new(Y, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YX => new(Y, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YY => new(Y, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YZ => new(Y, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_YW => new(Y, 0, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_Y_ => new(Y, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZX => new(Y, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZY => new(Y, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZZ => new(Y, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_ZW => new(Y, 0, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_Z_ => new(Y, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_WX => new(Y, 0, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_WY => new(Y, 0, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_WZ => new(Y, 0, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_WW => new(Y, 0, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y_W_ => new(Y, 0, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__X => new(Y, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__Y => new(Y, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__Z => new(Y, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y__W => new(Y, 0, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Y___ => new(Y, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXX => new(Z, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXY => new(Z, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXZ => new(Z, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXXW => new(Z, X, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXX_ => new(Z, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYX => new(Z, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYY => new(Z, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYZ => new(Z, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXYW => new(Z, X, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXY_ => new(Z, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZX => new(Z, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZY => new(Z, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZZ => new(Z, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZW => new(Z, X, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXZ_ => new(Z, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXWX => new(Z, X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXWY => new(Z, X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXWZ => new(Z, X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXWW => new(Z, X, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZXW_ => new(Z, X, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_X => new(Z, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_Y => new(Z, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_Z => new(Z, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX_W => new(Z, X, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZX__ => new(Z, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXX => new(Z, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXY => new(Z, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXZ => new(Z, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYXW => new(Z, Y, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYX_ => new(Z, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYX => new(Z, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYY => new(Z, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYZ => new(Z, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYYW => new(Z, Y, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYY_ => new(Z, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZX => new(Z, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZY => new(Z, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZZ => new(Z, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZW => new(Z, Y, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYZ_ => new(Z, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYWX => new(Z, Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYWY => new(Z, Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYWZ => new(Z, Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYWW => new(Z, Y, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZYW_ => new(Z, Y, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_X => new(Z, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_Y => new(Z, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_Z => new(Z, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY_W => new(Z, Y, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZY__ => new(Z, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXX => new(Z, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXY => new(Z, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXZ => new(Z, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZXW => new(Z, Z, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZX_ => new(Z, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYX => new(Z, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYY => new(Z, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYZ => new(Z, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZYW => new(Z, Z, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZY_ => new(Z, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZX => new(Z, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZY => new(Z, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZZ => new(Z, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZW => new(Z, Z, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZZ_ => new(Z, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZWX => new(Z, Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZWY => new(Z, Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZWZ => new(Z, Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZWW => new(Z, Z, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZW_ => new(Z, Z, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_X => new(Z, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_Y => new(Z, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_Z => new(Z, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ_W => new(Z, Z, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZZ__ => new(Z, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWXX => new(Z, W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWXY => new(Z, W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWXZ => new(Z, W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWXW => new(Z, W, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWX_ => new(Z, W, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWYX => new(Z, W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWYY => new(Z, W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWYZ => new(Z, W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWYW => new(Z, W, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWY_ => new(Z, W, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWZX => new(Z, W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWZY => new(Z, W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWZZ => new(Z, W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWZW => new(Z, W, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWZ_ => new(Z, W, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWWX => new(Z, W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWWY => new(Z, W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWWZ => new(Z, W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWWW => new(Z, W, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZWW_ => new(Z, W, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ZW_X => new(Z, W, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZW_Y => new(Z, W, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZW_Z => new(Z, W, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZW_W => new(Z, W, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ZW__ => new(Z, W, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XX => new(Z, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XY => new(Z, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XZ => new(Z, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_XW => new(Z, 0, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_X_ => new(Z, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YX => new(Z, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YY => new(Z, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YZ => new(Z, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_YW => new(Z, 0, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_Y_ => new(Z, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZX => new(Z, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZY => new(Z, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZZ => new(Z, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_ZW => new(Z, 0, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_Z_ => new(Z, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_WX => new(Z, 0, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_WY => new(Z, 0, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_WZ => new(Z, 0, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_WW => new(Z, 0, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z_W_ => new(Z, 0, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__X => new(Z, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__Y => new(Z, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__Z => new(Z, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z__W => new(Z, 0, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 Z___ => new(Z, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WXXX => new(W, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXXY => new(W, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXXZ => new(W, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXXW => new(W, X, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXX_ => new(W, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WXYX => new(W, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXYY => new(W, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXYZ => new(W, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXYW => new(W, X, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXY_ => new(W, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WXZX => new(W, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXZY => new(W, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXZZ => new(W, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXZW => new(W, X, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXZ_ => new(W, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WXWX => new(W, X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXWY => new(W, X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXWZ => new(W, X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXWW => new(W, X, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WXW_ => new(W, X, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WX_X => new(W, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WX_Y => new(W, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WX_Z => new(W, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WX_W => new(W, X, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WX__ => new(W, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WYXX => new(W, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYXY => new(W, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYXZ => new(W, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYXW => new(W, Y, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYX_ => new(W, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WYYX => new(W, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYYY => new(W, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYYZ => new(W, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYYW => new(W, Y, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYY_ => new(W, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WYZX => new(W, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYZY => new(W, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYZZ => new(W, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYZW => new(W, Y, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYZ_ => new(W, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WYWX => new(W, Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYWY => new(W, Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYWZ => new(W, Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYWW => new(W, Y, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WYW_ => new(W, Y, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WY_X => new(W, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WY_Y => new(W, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WY_Z => new(W, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WY_W => new(W, Y, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WY__ => new(W, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WZXX => new(W, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZXY => new(W, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZXZ => new(W, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZXW => new(W, Z, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZX_ => new(W, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WZYX => new(W, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZYY => new(W, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZYZ => new(W, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZYW => new(W, Z, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZY_ => new(W, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WZZX => new(W, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZZY => new(W, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZZZ => new(W, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZZW => new(W, Z, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZZ_ => new(W, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WZWX => new(W, Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZWY => new(W, Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZWZ => new(W, Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZWW => new(W, Z, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZW_ => new(W, Z, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WZ_X => new(W, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZ_Y => new(W, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZ_Z => new(W, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZ_W => new(W, Z, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WZ__ => new(W, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WWXX => new(W, W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWXY => new(W, W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWXZ => new(W, W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWXW => new(W, W, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWX_ => new(W, W, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WWYX => new(W, W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWYY => new(W, W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWYZ => new(W, W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWYW => new(W, W, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWY_ => new(W, W, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WWZX => new(W, W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWZY => new(W, W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWZZ => new(W, W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWZW => new(W, W, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWZ_ => new(W, W, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WWWX => new(W, W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWWY => new(W, W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWWZ => new(W, W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWWW => new(W, W, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WWW_ => new(W, W, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 WW_X => new(W, W, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WW_Y => new(W, W, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WW_Z => new(W, W, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WW_W => new(W, W, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 WW__ => new(W, W, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 W_XX => new(W, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_XY => new(W, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_XZ => new(W, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_XW => new(W, 0, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_X_ => new(W, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 W_YX => new(W, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_YY => new(W, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_YZ => new(W, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_YW => new(W, 0, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_Y_ => new(W, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 W_ZX => new(W, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_ZY => new(W, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_ZZ => new(W, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_ZW => new(W, 0, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_Z_ => new(W, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 W_WX => new(W, 0, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_WY => new(W, 0, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_WZ => new(W, 0, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_WW => new(W, 0, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W_W_ => new(W, 0, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 W__X => new(W, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W__Y => new(W, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W__Z => new(W, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W__W => new(W, 0, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 W___ => new(W, 0, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXX => new(0, X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXY => new(0, X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXZ => new(0, X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XXW => new(0, X, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XX_ => new(0, X, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYX => new(0, X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYY => new(0, X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYZ => new(0, X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XYW => new(0, X, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XY_ => new(0, X, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZX => new(0, X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZY => new(0, X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZZ => new(0, X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZW => new(0, X, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XZ_ => new(0, X, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _XWX => new(0, X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XWY => new(0, X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XWZ => new(0, X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XWW => new(0, X, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _XW_ => new(0, X, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_X => new(0, X, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_Y => new(0, X, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_Z => new(0, X, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X_W => new(0, X, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _X__ => new(0, X, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXX => new(0, Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXY => new(0, Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXZ => new(0, Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YXW => new(0, Y, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YX_ => new(0, Y, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYX => new(0, Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYY => new(0, Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYZ => new(0, Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YYW => new(0, Y, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YY_ => new(0, Y, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZX => new(0, Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZY => new(0, Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZZ => new(0, Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZW => new(0, Y, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YZ_ => new(0, Y, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _YWX => new(0, Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YWY => new(0, Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YWZ => new(0, Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YWW => new(0, Y, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _YW_ => new(0, Y, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_X => new(0, Y, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_Y => new(0, Y, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_Z => new(0, Y, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y_W => new(0, Y, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Y__ => new(0, Y, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXX => new(0, Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXY => new(0, Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXZ => new(0, Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZXW => new(0, Z, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZX_ => new(0, Z, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYX => new(0, Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYY => new(0, Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYZ => new(0, Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZYW => new(0, Z, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZY_ => new(0, Z, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZX => new(0, Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZY => new(0, Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZZ => new(0, Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZW => new(0, Z, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZZ_ => new(0, Z, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZWX => new(0, Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZWY => new(0, Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZWZ => new(0, Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZWW => new(0, Z, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _ZW_ => new(0, Z, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_X => new(0, Z, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_Y => new(0, Z, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_Z => new(0, Z, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z_W => new(0, Z, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _Z__ => new(0, Z, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _WXX => new(0, W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WXY => new(0, W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WXZ => new(0, W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WXW => new(0, W, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WX_ => new(0, W, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _WYX => new(0, W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WYY => new(0, W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WYZ => new(0, W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WYW => new(0, W, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WY_ => new(0, W, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _WZX => new(0, W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WZY => new(0, W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WZZ => new(0, W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WZW => new(0, W, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WZ_ => new(0, W, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _WWX => new(0, W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WWY => new(0, W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WWZ => new(0, W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WWW => new(0, W, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _WW_ => new(0, W, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 _W_X => new(0, W, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _W_Y => new(0, W, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _W_Z => new(0, W, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _W_W => new(0, W, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 _W__ => new(0, W, 0, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __XX => new(0, 0, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XY => new(0, 0, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XZ => new(0, 0, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __XW => new(0, 0, X, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __X_ => new(0, 0, X, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __YX => new(0, 0, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YY => new(0, 0, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YZ => new(0, 0, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __YW => new(0, 0, Y, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __Y_ => new(0, 0, Y, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZX => new(0, 0, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZY => new(0, 0, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZZ => new(0, 0, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __ZW => new(0, 0, Z, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __Z_ => new(0, 0, Z, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 __WX => new(0, 0, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __WY => new(0, 0, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __WZ => new(0, 0, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __WW => new(0, 0, W, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 __W_ => new(0, 0, W, 0);

	[EB(EBS.Never), DB(DBS.Never)] public I4 ___X => new(0, 0, 0, X);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___Y => new(0, 0, 0, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___Z => new(0, 0, 0, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ___W => new(0, 0, 0, W);
	[EB(EBS.Never), DB(DBS.Never)] public I4 ____ => new(0, 0, 0, 0);

#endregion

#region Three

	[EB(EBS.Never), DB(DBS.Never)] public I3 XXX => new(X, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXY => new(X, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXZ => new(X, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XXW => new(X, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XYX => new(X, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYY => new(X, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYZ => new(X, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XYW => new(X, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XZX => new(X, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZY => new(X, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZZ => new(X, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XZW => new(X, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 XWX => new(X, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XWY => new(X, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XWZ => new(X, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 XWW => new(X, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YXX => new(Y, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXY => new(Y, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXZ => new(Y, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YXW => new(Y, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YYX => new(Y, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYY => new(Y, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYZ => new(Y, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YYW => new(Y, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YZX => new(Y, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZY => new(Y, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZZ => new(Y, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YZW => new(Y, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 YWX => new(Y, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YWY => new(Y, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YWZ => new(Y, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 YWW => new(Y, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXX => new(Z, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXY => new(Z, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXZ => new(Z, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZXW => new(Z, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYX => new(Z, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYY => new(Z, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYZ => new(Z, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZYW => new(Z, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZX => new(Z, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZY => new(Z, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZZ => new(Z, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZZW => new(Z, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 ZWX => new(Z, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZWY => new(Z, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZWZ => new(Z, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 ZWW => new(Z, W, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 WXX => new(W, X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WXY => new(W, X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WXZ => new(W, X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WXW => new(W, X, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 WYX => new(W, Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WYY => new(W, Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WYZ => new(W, Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WYW => new(W, Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 WZX => new(W, Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WZY => new(W, Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WZZ => new(W, Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WZW => new(W, Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public I3 WWX => new(W, W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WWY => new(W, W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WWZ => new(W, W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I3 WWW => new(W, W, W);

#endregion

#region Two

	[EB(EBS.Never), DB(DBS.Never)] public I2 XX => new(X, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XY => new(X, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XZ => new(X, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I2 XW => new(X, W);

	[EB(EBS.Never), DB(DBS.Never)] public I2 YX => new(Y, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YY => new(Y, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YZ => new(Y, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I2 YW => new(Y, W);

	[EB(EBS.Never), DB(DBS.Never)] public I2 ZX => new(Z, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 ZY => new(Z, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 ZZ => new(Z, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I2 ZW => new(Z, W);

	[EB(EBS.Never), DB(DBS.Never)] public I2 WX => new(W, X);
	[EB(EBS.Never), DB(DBS.Never)] public I2 WY => new(W, Y);
	[EB(EBS.Never), DB(DBS.Never)] public I2 WZ => new(W, Z);
	[EB(EBS.Never), DB(DBS.Never)] public I2 WW => new(W, W);

#endregion

}