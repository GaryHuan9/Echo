using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Packed;

namespace Echo.Core.InOut.Images;

public static class ColorConverter
{
	const float LinearToGammaThreshold = 0.0031308f;
	const float GammaToLinearThreshold = 0.04045f;
	const float GammaMultiplier = 12.92f;

	/// <summary>
	/// Converts a <see cref="Float4"/> from linear space into sRGB.
	/// </summary>
	/// <remarks>This mapping is from [0, 1] to [0, 1].</remarks>
	public static Float4 LinearToGamma(in Float4 x)
	{
		Ensure.IsTrue(Float4.Zero <= x && x <= Float4.One);

		Vector128<float> useCurve = Sse.CompareGreaterThan(x.v, Vector128.Create(LinearToGammaThreshold));
		useCurve = Sse41.Insert(useCurve, useCurve, 0b0000_1000); //Set element for alpha to zero; no sRGB for alpha

		Float4 line = x * GammaMultiplier;
		line = new Float4(Sse41.Blend(line.v, x.v, 0b0000_1000)); //No multiplication for alpha
		if (Sse.MoveMask(useCurve) == 0) return line;             //Exit if all channel is under threshold

		//7th degree polynomial approximation
		Float4 c0 = (Float4)(-6.6597116468188869467222448794707645447488175705075E-6f);
		Float4 c1 = (Float4)(+4.4427348230833630174374526689007325330749154090881E-4f);
		Float4 c2 = (Float4)(-1.3463367636769897617288194169304915703833103179931E-2f);
		Float4 c3 = (Float4)(+0.3344302293385857383078985094471136108040809631347E-0f);
		Float4 c4 = (Float4)(+1.2090203751207984073090528909233398735523223876953E-0f);
		Float4 c5 = (Float4)(-0.9896019329901286631923085224116221070289611816406E-0f);
		Float4 c6 = (Float4)(+0.6460906882428189001998930507397744804620742797851E-0f);
		Float4 c7 = (Float4)(-0.1870566881827923633174748374585760757327079772949E-0f);

		Vector128<float> otherCurve = Sse.CompareLessThan(x.v, Vector128.Create(0.052358f));

		if ((Sse.MoveMask(otherCurve) & 0b111) != 0)
		{
			//Use another curve when x is under a threshold to improve accuracy
			c0 = new Float4(Sse41.BlendVariable(c0.v, Vector128.Create(-3.5176563415651659896295553234106014139914E-10f), otherCurve));
			c1 = new Float4(Sse41.BlendVariable(c1.v, Vector128.Create(+4.12718116450792364704688343357563695690259E-7f), otherCurve));
			c2 = new Float4(Sse41.BlendVariable(c2.v, Vector128.Create(-2.22584041208560444360248231632226634246762E-4f), otherCurve));
			c3 = new Float4(Sse41.BlendVariable(c3.v, Vector128.Create(+6.11728193670682818261497004641569219529628E-2f), otherCurve));
			c4 = new Float4(Sse41.BlendVariable(c4.v, Vector128.Create(+6.60140585528584278307562271947972476482391E-0f), otherCurve));
			c5 = new Float4(Sse41.BlendVariable(c5.v, Vector128.Create(-1.00133817063040396533324383199214935302734E+2f), otherCurve));
			c6 = new Float4(Sse41.BlendVariable(c6.v, Vector128.Create(+1.22280174368753318958624731749296188354492E+3f), otherCurve));
			c7 = new Float4(Sse41.BlendVariable(c7.v, Vector128.Create(-6.66398442910490484791807830333709716796875E+3f), otherCurve));
		}

		Float4 x2 = x * x;
		Float4 x4 = x2 * x2;

		Float4 curve;

		if (Fma.IsSupported)
		{
			Float4 p01_x0 = new Float4(Fma.MultiplyAdd(c1.v, x.v, c0.v));
			Float4 p23_x2 = new Float4(Fma.MultiplyAdd(c3.v, x.v, c2.v));
			Float4 p54_x4 = new Float4(Fma.MultiplyAdd(c5.v, x.v, c4.v));
			Float4 p67_x6 = new Float4(Fma.MultiplyAdd(c7.v, x.v, c6.v));

			Float4 p0123_x0 = new Float4(Fma.MultiplyAdd(p23_x2.v, x2.v, p01_x0.v));
			Float4 p4567_x4 = new Float4(Fma.MultiplyAdd(p67_x6.v, x2.v, p54_x4.v));
			curve = new Float4(Fma.MultiplyAdd(p4567_x4.v, x4.v, p0123_x0.v));
		}
		else
		{
			Float4 p01_x0 = c1 * x + c0;
			Float4 p23_x2 = c3 * x + c2;
			Float4 p54_x4 = c5 * x + c4;
			Float4 p67_x6 = c7 * x + c6;

			Float4 p0123_x0 = p23_x2 * x2 + p01_x0;
			Float4 p4567_x4 = p67_x6 * x2 + p54_x4;
			curve = p4567_x4 * x4 + p0123_x0;
		}

		curve /= x2 * x;

		return Float4.Clamp(new Float4(Sse41.BlendVariable(line.v, curve.v, useCurve)));
	}

	/// <summary>
	/// Converts a <see cref="Float4"/> from sRGB into linear space.
	/// </summary>
	/// <remarks>This mapping is from [0, 1] to [0, 1].</remarks>
	public static Float4 GammaToLinear(Float4 x)
	{
		Ensure.IsTrue(Float4.Zero <= x && x <= Float4.One);

		Vector128<float> useCurve = Sse.CompareGreaterThan(x.v, Vector128.Create(GammaToLinearThreshold));
		useCurve = Sse41.Insert(useCurve, useCurve, 0b0000_1000); //Set element for alpha to zero; no sRGB for alpha

		Float4 line = x * (1f / GammaMultiplier);
		line = new Float4(Sse41.Blend(line.v, x.v, 0b0000_1000)); //No multiplication for alpha
		if (Sse.MoveMask(useCurve) == 0) return line;             //Exit if all channel is under threshold

		//7th degree polynomial approximation
		Float4 c0 = (Float4)(+8.4588408523676651869227516300497882184572517871857E-4f);
		Float4 c1 = (Float4)(+3.5640521633940797086026464057795237749814987182617E-2f);
		Float4 c2 = (Float4)(+0.4811251066565690459597703920735511928796768188476E-0f);
		Float4 c3 = (Float4)(+0.8835210149152180614251506085565779358148574829101E-0f);
		Float4 c4 = (Float4)(-0.9021144130511945524730776924116071313619613647461E-0f);
		Float4 c5 = (Float4)(+0.9210126516132712826134820716106332838535308837890E-0f);
		Float4 c6 = (Float4)(-0.5723803071243608320273210665618535131216049194336E-0f);
		Float4 c7 = (Float4)(+0.1523851419389433969886482600486488081514835357666E-0f);

		Float4 x2 = x * x;
		Float4 x4 = x2 * x2;

		Float4 curve;

		if (Fma.IsSupported)
		{
			Float4 p01_x0 = new Float4(Fma.MultiplyAdd(c1.v, x.v, c0.v));
			Float4 p23_x2 = new Float4(Fma.MultiplyAdd(c3.v, x.v, c2.v));
			Float4 p54_x4 = new Float4(Fma.MultiplyAdd(c5.v, x.v, c4.v));
			Float4 p67_x6 = new Float4(Fma.MultiplyAdd(c7.v, x.v, c6.v));

			Float4 p0123_x0 = new Float4(Fma.MultiplyAdd(p23_x2.v, x2.v, p01_x0.v));
			Float4 p4567_x4 = new Float4(Fma.MultiplyAdd(p67_x6.v, x2.v, p54_x4.v));
			curve = new Float4(Fma.MultiplyAdd(p4567_x4.v, x4.v, p0123_x0.v));
		}
		else
		{
			Float4 p01_x0 = c1 * x + c0;
			Float4 p23_x2 = c3 * x + c2;
			Float4 p54_x4 = c5 * x + c4;
			Float4 p67_x6 = c7 * x + c6;

			Float4 p0123_x0 = p23_x2 * x2 + p01_x0;
			Float4 p4567_x4 = p67_x6 * x2 + p54_x4;
			curve = p4567_x4 * x4 + p0123_x0;
		}

		return Float4.Clamp(new Float4(Sse41.BlendVariable(line.v, curve.v, useCurve)));
	}

	/// <summary>
	/// Gathers the first byte of each element in a <see cref="Vector128{T}"/> into an <see cref="uint"/>.
	/// </summary>
	public static uint GatherBytes(Vector128<uint> value)
	{
		Ensure.IsTrue(value.GetElement(0) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(1) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(2) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(3) <= byte.MaxValue);

		// 000000WW 000000ZZ 000000YY 000000XX               original
		//       00 0000WW00 0000ZZ00 0000YY00 0000XX        shift by 3 bytes
		// 000000WW 0000WWZZ 0000ZZYY 0000YYXX               or with original
		//              0000 00WW0000 WWZZ0000 ZZYY0000 YYXX shift by 6 bytes
		// 000000WW 0000WWZZ 00WWZZYY WWZZYYXX               or with original

		value = Sse2.Or(value, Sse2.ShiftRightLogical128BitLane(value, 3));
		value = Sse2.Or(value, Sse2.ShiftRightLogical128BitLane(value, 6));
		return value.ToScalar();
	}

	/// <summary>
	/// Scatters an <see cref="uint"/> to the first byte of each element in a <see cref="Vector128{T}"/>.
	/// </summary>
	public static Vector128<uint> ScatterBytes(uint value)
	{
		//               00000000 00000000 00000000 WWZZYYXX original
		// 0000 00000000 00000000 0000WWZZ YYXX              shift by 6 bytes
		//               00000000 0000WWZZ YYXX0000 WWZZYYXX or with original
		//        000000 000000WW ZZYYXX00 00WWZZYY XX       shift by 3 bytes
		//               000000WW ZZYYWWZZ YYXXZZYY --ZZYYXX or with original

		Vector128<uint> v = Vector128.CreateScalar(value);
		v = Sse2.Or(v, Sse2.ShiftLeftLogical128BitLane(v, 6));
		v = Sse2.Or(v, Sse2.ShiftLeftLogical128BitLane(v, 3));
		return Sse2.And(v, Vector128.Create((uint)byte.MaxValue));
	}

	static Vector128<uint> Float4ToBytes(Float4 value)
	{
		Ensure.IsTrue(Float4.Zero <= value && value <= Float4.One);

		Float4 scaled = value * byte.MaxValue;
		return Sse2.ConvertToVector128Int32(scaled.v).AsUInt32();
	}

	static Float4 BytesToFloat4(Vector128<uint> value)
	{
		Ensure.IsTrue(value.GetElement(0) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(1) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(2) <= byte.MaxValue);
		Ensure.IsTrue(value.GetElement(3) <= byte.MaxValue);

		var converted = Sse2.ConvertToVector128Single(value.AsInt32());
		return new Float4(converted) * (1f / byte.MaxValue);
	}

	//sRGB approximation using 7th degree polynomial: https://www.desmos.com/calculator/gfb8amzlzo
	//Coefficients calculated using sollya:
	// f = ((x+0.055)/1.055)^2.4;
	// p = fpminimax(f, 7, [|D...|], [0.04045;1]);
	// print("Error:", dirtyinfnorm((f-p)/f, [0.04045;1]));
	// print(canonical(p));
	//
	// t = 0.052358;
	// r = 3;
	// f = 1.055 * x^(1/2.4) - 0.055;
	//
	// p = fpminimax(f(x)*(x^r), 7, [|D...|], [0.0031308;t])/x^r;
	// print("Error:", dirtyinfnorm((f-p)/f, [0.0031308;t]));
	// print(canonical(p));
	//
	// p = fpminimax(f(x)*(x^r), 7, [|D...|], [t;1])/x^r;
	// print("Error:", dirtyinfnorm((f-p)/f, [t;1]));
	// print(canonical(p));
}