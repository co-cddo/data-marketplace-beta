using System.ComponentModel;

namespace Cddo.Data.Marketplace.Api.Dto;

public enum OrganisationFormat
{
    [Description("Ad-hoc advisory group")]
    AdHocAdvisoryGroup,

    [Description("Advisory non-departmental public body")]
    AdvisoryNonDepartmentalPublicBody,

    [Description("Agencies and other public bodies")]
    AgenciesAndOtherPublicBodies,

    [Description("Devolved administrations")]
    DevolvedAdministrations,

    [Description("Executive agency")]
    ExecutiveAgency,

    [Description("Executive non-departmental public body")]
    ExecutiveNonDepartmentalPublicBody,

    [Description("Executive office")]
    ExecutiveOffice,

    [Description("High profile groups")]
    HighProfileGroups,

    [Description("Ministerial departments")]
    MinisterialDepartments,

    [Description("Non-ministerial departments")]
    NonMinisterialDepartments,

    [Description("Other")]
    Other,

    [Description("Public corporations")]
    PublicCorporations,

    [Description("Tribunal")]
    Tribunal
}