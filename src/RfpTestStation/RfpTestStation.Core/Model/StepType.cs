namespace RfpTestStation.Core.Model
{
    public enum StepType
    {
        Unknown,
        Action,
        NumericLimitTest,
        StringValueTest,
        PassFailTest,
        MultipleNumericLimitTest,
        Wait,
        If,
        Else,
        ElseIf,
        While,
        For,
        ForEach,
        End,
        SequenceCall,
        Statement,
        Label,
        Goto,
        MessagePopup
    }
}
