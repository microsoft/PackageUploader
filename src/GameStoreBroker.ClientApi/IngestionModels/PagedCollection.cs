using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class PagedCollection<T>
{
    /// <summary>
    /// Gets the current page of elements.
    /// </summary>
    [JsonProperty(PropertyName = "value")]
    public IList<T> Value { get; private set; }
}