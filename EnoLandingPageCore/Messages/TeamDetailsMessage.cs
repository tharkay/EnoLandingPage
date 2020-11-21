﻿namespace EnoLandingPageCore.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using EnoLandingPageCore.Database;

    public record TeamDetailsMessage(
        long Id,
        bool Confirmed,
        string TeamName,
        string? VpnConfig,
        string? RootPassword,
        string? ExternalIpAddress,
        string? InternalIpAddress,
        LandingPageVulnboxStatus? VulnboxStatus);
}
