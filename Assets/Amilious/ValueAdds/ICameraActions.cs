using UnityEngine.InputSystem;

namespace Amilious.ValueAdds
{
	public interface ICameraActions
	{
		void OnMove(InputValue value);
		void OnFly(InputValue value);
		void OnSprint(InputValue value);
		void OnRotate(InputValue value);
		void OnMouseX(InputValue value);
		void OnMouseY(InputValue value);
		
	}
}