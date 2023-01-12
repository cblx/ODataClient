namespace Cblx.Dynamics;
public class PicklistOption : PicklistOption<int> {}

public class PicklistOption<T> where T : struct
{
    public required string Text { get; set; }
    public T Value { get; set; }
}