namespace Echo.Core.Scenic.Lights;

/// <summary>
/// A non generic variant of <see cref="ILightSource{T}"/>; should not be directly implemented.
/// </summary>
public interface ILightSource { }

/// <summary>
/// Implemented by <see cref="LightEntity"/>s which are sources that can produce <see cref="IPreparedLight"/>.
/// </summary>
/// <typeparam name="T">The type of the <see cref="IPreparedLight"/>
/// that this <see cref="ILightSource{T}"/> can produce.</typeparam>
public interface ILightSource<out T> : ILightSource where T : IPreparedLight
{
	public T Extract();
}