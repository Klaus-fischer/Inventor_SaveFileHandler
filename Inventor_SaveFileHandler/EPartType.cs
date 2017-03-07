// <copyright file="EPartType.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    /// <summary>
    /// Enumeration of user choice of possible part types.
    /// </summary>
    public enum EPartType
    {
        /// <summary>
        /// Type for own manufacturing.
        /// </summary>
        MakePart,

        /// <summary>
        /// Type for parts from customers.
        /// </summary>
        CustomerPart,

        /// <summary>
        /// Type for party to buy.
        /// </summary>
        BuyPart
    }
}
