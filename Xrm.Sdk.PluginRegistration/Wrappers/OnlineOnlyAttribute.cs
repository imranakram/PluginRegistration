namespace Xrm.Sdk.PluginRegistration.Wrappers
{
    using System;

    /// <summary>
    /// Marks a property that only makes sense for Dataverse online. The properties grid filters
    /// on <see cref="No"/> for on-premises organizations, which hides the marked properties.
    /// Properties without the attribute get <see cref="Default"/> (No), so they always show.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OnlineOnlyAttribute : Attribute
    {
        #region Public Fields

        public static readonly OnlineOnlyAttribute Yes = new OnlineOnlyAttribute(true);
        public static readonly OnlineOnlyAttribute No = new OnlineOnlyAttribute(false);
        public static readonly OnlineOnlyAttribute Default = No;

        #endregion Public Fields

        #region Public Constructors

        public OnlineOnlyAttribute(bool onlineOnly)
        {
            OnlineOnly = onlineOnly;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool OnlineOnly { get; }

        #endregion Public Properties

        #region Public Methods

        public override bool Equals(object obj)
        {
            return obj is OnlineOnlyAttribute other && other.OnlineOnly == OnlineOnly;
        }

        public override int GetHashCode()
        {
            return OnlineOnly.GetHashCode();
        }

        public override bool IsDefaultAttribute()
        {
            return Equals(Default);
        }

        #endregion Public Methods
    }
}
