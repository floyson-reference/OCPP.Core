﻿/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using OCPP.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    /// <summary>
    /// Encapsulates the data of a connected chargepoint in the server
    /// Attention: Identical class in OCPP.Server (shoud be external common...)
    /// </summary>

    public class ChargePointStatus
    {
        public ChargePointStatus()
        {
        }

        /// <summary>
        /// ID of chargepoint
        /// </summary>
        [Newtonsoft.Json.JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Name of chargepoint
        /// </summary>
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// OCPP protocol version
        /// </summary>
        [Newtonsoft.Json.JsonProperty("protocol")]
        public string Protocol { get; set; }

        /// <summary>
        /// Status of first (or only) charge connector
        /// </summary>
        public ConnectorStatus EVSE1Status { get; set; }

        /// <summary>
        /// Status of second  charge connector (currently not used)
        /// </summary>
        public ConnectorStatus EVSE2Status { get; set; }

        /// <summary>
        /// Details about current charge porcess (if charging)
        /// </summary>
        public ChargingData ChargingDataEVSE1 { get; set; }

        /// <summary>
        /// Details about current charge porcess (if charging)
        /// </summary>
        public ChargingData ChargingDataEVSE2 { get; set; }
    }

    /// <summary>
    /// Encapsulates details about a charging process
    /// </summary>
    public class ChargingData
    {
        /// <summary>
        /// Current charge rate in kW
        /// </summary>
        public double? ChargeRateKW { get; set; }

        /// <summary>
        /// Charged energy in kWh
        /// </summary>
        public double? ChargedEnergyKWH { get; set; }

        /// <summary>
        /// StateOfCharges in percent
        /// </summary>
        public double? SoC { get; set; }
    }

    public enum ConnectorStatus
    {
        [System.Runtime.Serialization.EnumMember(Value = @"")]
        Undefined = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Available")]
        Available = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Occupied")]
        Occupied = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Unavailable")]
        Unavailable = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Faulted")]
        Faulted = 4
    }
}
