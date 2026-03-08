using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Core.Models;

namespace Restaurant.Core.SharedModels
{
    public class ReservationStatusConverter : IPropertyConverter
    {
        public DynamoDBEntry ToEntry(object value)
            => new Primitive(value.ToString());

        public object FromEntry(DynamoDBEntry entry)
            => Enum.Parse<ReservationStatus>(entry.AsString());
    }
}
