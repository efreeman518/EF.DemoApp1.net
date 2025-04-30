using Microsoft.Extensions.VectorData;

namespace Console.AI1.Model;
public class Item
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [VectorStoreRecordData]
    public string Name { get; set; } = null!;

    [VectorStoreRecordData]
    public string Description { get; set; } = null!;

    [VectorStoreRecordData]
    public TimeSpan StartTime { get; set; }

    [VectorStoreRecordData]
    public TimeSpan EndTime { get; set; }

    [VectorStoreRecordData]
    public CommonEnum EnumValue { get; set; }

    [VectorStoreRecordData]
    public List<CommonEnum> EnumList { get; set; } = [];

    [VectorStoreRecordVector(Dimensions:384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }

}
