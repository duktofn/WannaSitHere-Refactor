namespace Game.Events
{
    /// <summary>
    /// Presentation-facing audio cue identifiers. The enum intentionally contains no
    /// Unity audio objects so gameplay and application assemblies can raise cues without
    /// knowing how they are rendered.
    /// </summary>
    public enum AudioCueId
    {
        ButtonClick,
        Transition,
        BoosterUsed,
        PersonHappy,
        Win,
        Lose,
        Claim,
        Spend
    }
}
