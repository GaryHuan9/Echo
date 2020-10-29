using CodeHelpers.Vectors;
using ForceRenderer.Objects;

namespace ForceRenderer.Modifiers
{
	public class RepetitionModifier : Modifier
	{
		public RepetitionModifier(SceneObject encapsulated, Float3 gap) : base(encapsulated) => Gap = gap;

		Float3 gap;
		Float3 half;

		public Float3 Gap
		{
			get => gap;
			set
			{
				gap = value;
				half = value / 2f;
			}
		}

		public override float SignedDistance(Float3 point)
		{
			Float3 q = Mod(point - Position + half, Gap) - half;
			return encapsulated.SignedDistance(q);
		}

		static Float3 Mod(Float3 value, Float3 other) => value - other * (value / other).FlooredFloat;
	}
}