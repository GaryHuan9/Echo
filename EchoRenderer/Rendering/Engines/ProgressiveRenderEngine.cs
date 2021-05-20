using System;
using System.Threading;
using CodeHelpers.Threads;

namespace EchoRenderer.Rendering.Engines
{
	public class ProgressiveRenderEngine
	{
		public ProgressiveRenderProfile CurrentProfile { get; private set; }

		int _currentState;

		public State CurrentState
		{
			get => (State)InterlockedHelper.Read(ref _currentState);
			private set => Interlocked.Exchange(ref _currentState, (int)value);
		}

		public void Begin(ProgressiveRenderProfile profile)
		{
			if (CurrentState == State.rendering) throw new Exception($"Must change to {nameof(State.waiting)} with {nameof(Stop)} before starting!");

			profile.Validate();
			CurrentProfile = profile;


		}

		public void Stop()
		{
			CurrentState = State.waiting;
			CurrentProfile = null;
		}

		public enum State
		{
			waiting,
			rendering
		}
	}
}