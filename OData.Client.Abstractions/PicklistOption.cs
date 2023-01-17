namespace Cblx.Dynamics;
public class PicklistOption : PicklistOption<int> {}

public class PicklistOption<T> : PicklistOptionBase where T : struct
{
    public T Value => (T)(object)RawValue;
}

public abstract class PicklistOptionBase
{
    public required string Text { get; set; }
    public required int RawValue { get; set; }
}