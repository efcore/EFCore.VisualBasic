using Microsoft.EntityFrameworkCore.Storage.Json;
using System;
using System.Text.Json;

namespace EntityFrameworkCore.VisualBasic.Scaffolding.Internal
{
    public sealed class MyJsonGuidReaderWriter : JsonValueReaderWriter<Guid>
    {
        public override Guid FromJsonTyped(ref Utf8JsonReaderManager manager)
            => manager.CurrentReader.GetGuid();

        public override void ToJsonTyped(Utf8JsonWriter writer, Guid value)
            => writer.WriteStringValue(value);
    }
}
