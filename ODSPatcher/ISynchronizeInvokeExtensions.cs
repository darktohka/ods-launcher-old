using System;
using System.ComponentModel;

namespace ODSPatcher
{
    public static class ISynchronizeInvokeExtensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                try
                {
                    @this.Invoke(action, new object[] { @this });
                }
                catch (Exception) { }
            }
            else
            {
                action(@this);
            }
        }
    }
}
