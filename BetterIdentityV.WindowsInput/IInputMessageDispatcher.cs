using Vanara.PInvoke;

namespace BetterIdentityV.WindowsInput;

internal interface IInputMessageDispatcher
{
    public void DispatchInput(User32.INPUT[] inputs);
}