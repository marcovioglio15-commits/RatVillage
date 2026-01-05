using System;
using UnityEngine;

namespace EmergentMechanics
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EM_IdSelectorAttribute : PropertyAttribute
    {
        #region Fields
        private readonly EM_IdCategory[] categories;
        #endregion

        #region Public Properties
        public EM_IdCategory[] Categories
        {
            get
            {
                return categories;
            }
        }
        #endregion

        #region Constructors
        public EM_IdSelectorAttribute(params EM_IdCategory[] categories)
        {
            this.categories = categories;
        }
        #endregion
    }
}
