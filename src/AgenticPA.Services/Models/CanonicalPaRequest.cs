namespace AgenticPA.Services.Models;

public record CanonicalPaRequest(
    string MemberId,
    string Cpt,
    string RequestingNpi,
    string FacilityNpi,
    string Icd10,
    int ConservativeTreatmentWeeks,
    string Notes);
