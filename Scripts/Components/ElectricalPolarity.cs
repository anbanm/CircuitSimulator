using UnityEngine;

/// <summary>
/// Electrical polarity enum for proper educational terminology
/// </summary>
public enum ElectricalPolarity
{
    Positive,    // Battery positive, DC positive
    Negative,    // Battery negative, DC negative
    Anode,       // LED/Diode anode (positive side)
    Cathode,     // LED/Diode cathode (negative side)
    Neutral,     // Non-polarized (resistors, bulbs in basic circuits)
    Live,        // AC live/hot wire
    Ground,      // Earth ground
    Reference    // Reference point (0V)
}