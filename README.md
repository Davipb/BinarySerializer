BinarySerializer
================

A .NET declarative serialization framework for controlling formatting of data at the byte level.  BinarySerializer is designed to make working with binary formats and protocols fast and simple.  The .NET Standard Library is available on [nuget](https://www.nuget.org/packages/BinarySerializer).  I am no longer actively maintaining the [Portable Class Library](https://www.nuget.org/packages/BinarySerializer/6.0.3).

### What BinarySerializer is not ###

BinarySerializer is not a competitor to protobuf, MessagePack, or any other number of fixed-format serializers.  While fast, BinarySerializer is designed to be first and foremost flexible in terms of the underlying data format.  If you don't like the way protobuf is serializing your data, you're stuck with it.  With BinarySerializer you can define precisely how your data is formatted using types, bindings, converters, and code.

### Field Ordering ###

There is no completely reliable way to get member ordering from the CLR so <code>FieldOrder</code> attributes are required on all classes with more than one field or property.  By convention, base classes are serialized first followed by any derived classes.  For example, the following <code>DerivedClass</code> will serialize in the order A, B, C.

```c#
public class BaseClass
{
    public int A { get; set; }
}

public class DerivedClass : BaseClass
{
    [FieldOrder(0)]
    public int B { get; set; }

    [FieldOrder(1)]
    public int C;
}
```

```c#
var stream = new MemoryStream();
var serializer = new BinarySerializer();
var derivedClass = new DerivedClass();
serializer.Serialize(stream, derivedClass);
```

Note that we're using properties and fields interchangeably as they are treated equivalently by the serializer.

### Binding ###

The most powerful feature of BinarySerializer is the ability to bind attributes to other fields in the object graph.  Using the available attributes this approach can allow for interop with complex formats and protocols.  One of the simplest examples of this is field length binding.

```c#
public class Person
{
    [FieldOrder(0)]
    public byte NameLength { get; set; }

    [FieldOrder(1)]
    [FieldLength("NameLength")]
    public string Name { get; set; }
}
```

```c#
var person = new Person { Name = "Alice" };
serializer.Serialize(stream, person);
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/LengthBinding.png" />
</p>

Note that it is not necessary that NameLength contain the length of the Name field as that value will be computed during serialization and updated in the serialized graph.  During deserialization the NameLength value will be used to correctly deserialize the Name field.

Length can also be specified at an object level.  See the [FieldLength](#fieldlengthattribute) section for more examples.


Attributes
----------

There are a number of attributes that can be used to control the serialization of fields.

* [Ignore](#ignoreattribute)
* [FieldOrder](#fieldorderattribute)
* [FieldLength](#fieldlengthattribute)
* [FieldCount](#fieldcountattribute)
* [FieldAlignment](#fieldalignmentattribute)
* [FieldScale](#fieldscaleattribute)
* [FieldEndianness](#fieldendiannessattribute)
* [FieldEncoding](#fieldencodingattribute)
* [FieldValue](#fieldvalueattribute)
* [FieldChecksum](#fieldchecksumattribute)
* [FieldCrc16](#fieldcrc16attribute)
* [FieldCrc32](#fieldcrc32attribute)
* [FieldOffset](#fieldoffsetattribute)
* [Subtype](#subtypeattribute)
* [SubtypeFactory](#subtypefactoryattribute)
* [SubtypeDefault](#subtypeattribute)
* [SerializeAs](#serializeasattribute)
* [SerializeAsEnum](#serializeasenumattribute)
* [SerializeWhen](#serializewhenattribute)
* [SerializeWhenNot](#serializewhennotattribute)
* [SerializeUntil](#serializeuntilattribute)
* [ItemLength](#itemlengthattribute)
* [ItemSubtype](#itemsubtypeattribute)
* [ItemSubtypeFactory](#itemsubtypeattribute)
* [ItemSubtypeDefault](#itemsubtypeattribute)
* [ItemSerializeUntil](#itemserializeuntilattribute)

### IgnoreAttribute ###

Any field or property with an Ignore attribute will not be included in serialization or deserialization.  These fields can still be used in bindings, however properties will be treated as normal fields.  If some calculation on a binding source is required, this can be accomplished with a binding converter.

### FieldOrderAttribute ###

This attribute is required on any field or property in a class with more than one field or property.  Only the relative order value matters; for example, field ordering can be zero-based, one-based, prime numbers only, etc.  In the case of a class inheriting from a base, base fields are serialized before derived values irrespective of field order numbers.  In the following example the field A will be serialized first, followed by B and then C.  Note that the base class does not need to specify field ordering as there is only one field.

```c#
public class BaseClass
{
    public int A { get; set; }
}

public class DerivedClass : BaseClass
{
    [FieldOrder(0)]
    public int B { get; set; }

    [FieldOrder(1)]
    public int C { get; set; }
}
```

### FieldLengthAttribute ###

FieldLength can be used to specify either a bound or constant field length.  Field lengths can apply to anything that is sizable including strings, arrays, lists, streams, and even objects.

For constant length fields, the serialized field length will always result in the specified length, either by limiting the serialization operation or padding out the result with zeros.  For bound length fields, the source will be updated with the serialized length.  Typically source fields are value types such as integers but value converters may also be used to update other types.

```c#
public class Person
{
    [FieldLength(32)]
    public string Name { get; set; }
}
```

Alternatively, the length of the Name field could be bound to a NameLength field:

```c#
public class Person
{
    [FieldOrder(0)]
    public byte NameLength { get; set; }

    [FieldOrder(1)]
    [FieldLength("NameLength")]
    public string Name { get; set; }
} 
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/LengthBinding.png" />
</p>

In some cases you may want to limit a collection of items by the total serialized length.  Note that we are *not* restricting the number of items in the collection here, but the serialized length in bytes.  To restrict the number of items in a collection use the FieldCount attribute.

```c#
public class Directory
{
    [FieldOrder(0)]
    public byte NamesLength { get; set; }

    [FieldOrder(1)]
    [FieldLength("NamesLength")]
    public List<string> Names { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/CollectionLengthBinding.png" />
</p>

If you want to enforce the size of an entire object, you can write:

```c#
public class Person
{
    [FieldOrder(0)]
    public string FirstName { get; set; }

    [FieldOrder(1)]
    public string LastName { get; set; }
}

public class PersonContainer
{
    [FieldLength(24)]
    public Person Person { get; set; }
}
```

Note that if the field length is constant Person will *always* be 24 bytes long and will be padded out if the serialized length of Person is less than 24.  However, if the length is bound to a field such as "PersonLength" then the actual length of Person will take precedence and PersonLength will be updated accordingly during serialization.

```c#
public class PersonContainer
{
    [FieldOrder(0)]
    public int PersonLength { get; set; } // set to the length of Person during serialization.

    [FieldOrder(1)]
    [FieldLength("PersonLength")]
    public Person Person { get; set; }
}
```

Some formats and protocols will define a set of fields of specified size, with "optional" trailing fields.  In the following example, EntryLength will either be 32 or 36, depending on whether or not Age is specified.  

```c#
public class Person
{
    [FieldOrder(0)]
    [FieldLength(32)]
    public string Name { get; set; }

    [FieldOrder(1)]
    public int? Age { get; set; }
}

public class PersonEntry
{
    [FieldOrder(0)]
    public int EntryLength { get; set; }

    [FieldOrder(1)]
    [FieldLength("EntryLength")]
    public Person Person { get; set; }
}
```

If age is null during serialization, the framework will update EntryLength to be 32, or 36 if Age is present.  If EntryLength is 32 during deserialization, the framework will return a null value for Age.  If EntryLength is 36, the framework will deserialize the Age value.

### FieldCountAttribute ###

The FieldCount attribute is used to define how many items are contained in a collection.  In practice, this is either an array or a list with a single generic argument.

```c#
public class Directory
{
    [FieldOrder(0)]
    public byte EntryCount { get; set; }

    [FieldOrder(1)]
    [FieldCount("EntryCount")]
    public List<Entry> Entries { get; set; }
}
```

*Note there is the special case of a byte array, for which FieldLength and FieldCount attributes are interchangeable.*

By default strings will be serialized as null terminated, which allows for this construction:

```c#
public class Directory
{
    [FieldOrder(0)]
    public byte NameCount { get; set; }

    [FieldOrder(1)]
    [FieldCount("NameCount")]
    public List<string> Names { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/CountBinding.png" />
</p>

### FieldAlignmentAttribute ###

The FieldAlignment attribute can be used to force field alignment to a constant or bound (probably unusual) value while keeping bound field lengths intact.  For example, take a construct wherein the length of a complex field is specified, but with the qualifier that irrespective of the specified length the grouping will be 32-bit aligned.  In that case, we may need to specify:

```c#
public class Entry
{
    [FieldOrder(0)]
    public byte Length { get; set; }

    [FieldOrder(1)]
    [FieldAlignment(4)]
    [FieldLength("Length")]
    public string Value { get; set; }
}
```

Let's say Value is set to 'hi'.  The framework will compute two (2) for the value of Length.  However, the Value field will be forcefully aligned to 32-bit boundaries and will therefore start at byte 5 and occupy 4 bytes.  This alignment will not affect the string value, which will still be "hi" (not, for example, "hi\0\0").

By default FieldAlignment will align both the "left" and "right" boundary of the field.  However, you can override this behavior by setting the FieldAlignmentMode to LeftOnly or RightOnly.  In advanced cases, left and right alignment values can be mixed with multiple attributes.

FieldAlignment is not inherited by child fields.

```c#
public class Entry
{
    [FieldOrder(0)]
    public byte Length { get; set; }

    [FieldOrder(1)]
    [FieldAlignment(4, FieldAlignmentMode.LeftOnly)]
    [FieldAlignment(2, FieldAlignmentMode.RightOnly)]
    [FieldLength("Length")]
    public string Value { get; set; }
}
```

In this example the Value field will always start on a modulo 4 byte boundary but terminate on a modulo 2 byte boundary with respect to the parent.

### FieldScaleAttribute ###

The FieldScale attribute can be used to specify value-type fields that encode some scaled representation of the actual value.  For example, what would normally be a floating point value could be stored as an integer.

```c#
public class Coordinates
{
    [FieldOrder(0)]
    [FieldScale(10000000)]
    [SerializeAs(SerializedType.Int4)]
    public double Longitude { get; set; }

    [FieldOrder(1)]
    [FieldScale(10000000)]
    [SerializeAs(SerializedType.Int4)]
    public double Latitude { get; set; }
}
```

Note that the definition includes SerializeAs attributes specifying that the underlying values be represented as signed integers.

### FieldEndiannessAttribute ###

The FieldEndianness attribute allows for the dynamic switching of endianness during deserialization.  This can be useful for dealing with formats which specify endianness through the use of magic numbers or values.  The binding constructor for FieldEndianness requires the type of a value converter, which returns an Endianness value be specified.  This attribute will be inherited by all child fields unless overwritten.

```c#
public class Packet
{
    [FieldOrder(0)]
    public uint Endianness { get; set; }

    [FieldOrder(1)]
    [FieldEndianness("Endianness", typeof(EndiannessConverter))]
    public ushort Value { get; set; }
}
```

In order to convert the Endianness "magic" number field into Endianness, we define a converter.

```c#
public class EndiannessConverter : IValueConverter
{
    private const uint LittleEndiannessMagic = 0x1A2B3C4D;
    private const uint BigEndiannessMagic = 0x4D3C2B1A;

    public object Convert(object value, object parameter, BinarySerializationContext context)
    {
        var indicator = System.Convert.ToUInt32(value);

        if (indicator == LittleEndiannessMagic)
        {
            return BinarySerialization.Endianness.Little;
        }
        else if (indicator == BigEndiannessMagic)
        {
            return BinarySerialization.Endianness.Big;
        }

        throw new InvalidOperationException("Invalid endian magic");
    }

    public object ConvertBack(object value, object parameter, BinarySerializationContext context)
    {
        throw new NotSupportedException();
    }
}
```

FieldEndianness is one of the stranger attributes in that in some instances the framework will defer evaluation of fields of bound endianness.  This can be useful in formats which declare endianness for specific fields even after those fields have already been encountered during deserialization.  Although odd, this may not actually represent an issue in the format, however it does mean that in these cases it is not safe to interpret the value of such a field until the "last possible moment".  Take the following example:

```c#
public class Header
{
    [FieldOrder(0)]
    [Endianness("ByteOrderIndicator", Converter = typeof(EndiannessConverter)]
    public int Length { get; set; }
	
    [FieldOrder(1)]
    public int ByteOrderIndicator { get; set; }
	
    [FieldOrder(2)]
    [FieldLength("Length")]
    public string Value { get; set; }
}
```

In this example the value of the Length field will not be evaluated until after the endianness has been determined.  Note that if the order of the last two fields were reversed the serializer would throw an exception as the resulting graph would be impossible to resolve. 

### FieldEncodingAttribute ###

Similar to the FieldEndianness attribute, the FieldEncoding attribute can be used to specify the string encoding for a field.  This attribute will be inherited by all child fields unless overridden.

### FieldValueAttributeBase ###

The FieldValueAttributeBase class is an abstract class that allows for the computation of complex fields based on either the value or serialized value of another field.  This can be used to create hashes, checksums, and other complex fields.  For information on custom FieldValue attributes, see [below](#extending-fieldvalue-attributes).

### FieldValueAttribute ###

This is the most trivial example of a FieldValue attribute and will simply copy the value of one field to another.

### FieldChecksumAttribute ###

The FieldChecksum attribute is a built-in extension of the FieldValueAttributeBase that allows for the computation of an 8-bit checksum.  The checksum can be configured with one of three modes: 2's complement, modulo 256, or xor.

*Note that this attribute is only used during serialization.  The checksum is not checked during deserialization.*

```c#
public class Packet
{
    [FieldOrder(0)]
    public int Length { get; set; }

    [FieldOrder(1)]
    [FieldLength("Length")]
    [FieldChecksum("Checksum", Mode = ChecksumMode.Xor)]
    public byte[] Data { get; set; }

    [FieldOrder(2)]
    public byte Checksum { get; set; }
}
```

### FieldCrc16Attribute ###

The FieldCrc16 attribute is a built-in extension of the FieldValueAttributeBase that allows for the computation of an unsigned 16-bit checksum.

*Note that this attribute is only used during serialization.  The CRC is not checked during deserialization.*

```c#
public class Packet
{
    [FieldOrder(0)]
    public int Length { get; set; }

    [FieldOrder(1)]
    [FieldLength("Length")]
    [FieldCrc16("Crc")]
    public byte[] Data { get; set; }

    [FieldOrder(2)]
    public ushort Crc { get; set; }
}
```

Note that the attribute can also be used on complex types to calculate the checksum over sets of fields.  The attribute can be configured by specifing the various properties, including the polynomial and the initial value.

```c#
public class Packet
{
    [FieldOrder(0)]
    public int Length { get; set; }

    [FieldOrder(1)]
    [FieldLength("Length")]
    [FieldCrc16("Crc", InitialValue = 0, IsDataReflected = false, IsRemainderReflected = false)]
    public Internal Internal { get; set; }

    [FieldOrder(2)]
    public ushort Crc { get; set; }
}
```

Also note that the target field must be an unsigned short, or unsigned int in the case of FieldCrc32.


### FieldCrc32Attribute ###

The FieldCrc32 is identical to the FieldCrc16 with the difference that it operates on an unsigned 32-bit field and with appropriate default algorithm values.

### FieldOffsetAttribute ###

The FieldOffset attribute should be used sparingly but can be used if an absolute offset is required.  In most cases implicit offset (e.g. just define the structure) is preferable.  After moving to the offset the serializer will reset to the origin so subsequent fields must manage their own offsets.  This attribute is not supported when serializing to non-seekable streams.

### SubtypeAttribute ###

The Subtype attribute allows dynamic switching of subtypes based on a binding.  In this example the specified subtypes have a base type of Frame.

```c#
public class Packet
{
    [FieldOrder(0)]
    public FrameType FrameType { get; set; }

    [FieldOrder(1)]
    [Subtype("FrameType", FrameType.Message, typeof(MessageFrame)]
    [Subtype("FrameType", FrameType.Control, typeof(ControlFrame)]
    [Subtype("FrameType", FrameType.Trigger, typeof(TriggerFrame)]
    [SubtypeDefault(typeof(UnknownFrame))]
    public Frame Frame { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/SubtypeBinding.png" />
</p>

It is not necessary that FrameType be correct during serialization; it will be updated with the appropriate value based on the instantiated type.  During deserialization the FrameType field will be used to construct the correct type.

The Subtype attribute can be used with the FieldLength attribute to write forward compatible processors.  Take the example of PNG, which uses "chunks" of data that may be able to be skipped even if they aren't understood.

It is also possible to specify different subtypes for target and source bindings.  This is an example from the u-blox protocol:

```c#
[Subtype("MessageId", MessageId.NAV_PVT, typeof(NavPvt))]
[Subtype("MessageId", MessageId.NAV_PVT, typeof(NavPvtPoll), BindingMode = BindingMode.OneWay)]
public PacketPayload Payload { get; set; }
```

The NavPvt message is only ever used from device to host whereas the NavPvtPoll message is used from the host to device.  Both subtypes have an identifier of MessageId.NAV_PVT, which would normally cause abmiguity during deserialization.  However, because the NavPvtPoll subtype binding specifies OneWay, only the NavPvt subtype is available during deserialization.

### SubtypeFactoryAttribute ###

For situations where it is not desirable to statically declare subtypes either for large numbers of or dynamically loaded subtypes, a factory may be specified.  In these instances any Subtype attributes will always be evaluated first and the factory will be used as fallback, followed by the default subtype, if specified.

```c#
public class Packet
{
    [FieldOrder(0)]
    public FrameType FrameType { get; set; }

    [FieldOrder(1)]
    [Subtype("FrameType", FrameType.Message, typeof(MessageFrame)]
    [Subtype("FrameType", FrameType.Control, typeof(ControlFrame)]
    [Subtype("FrameType", FrameType.Trigger, typeof(TriggerFrame)]
    [SubtypeFactory("FrameType", typeof(FrameFactory))]
    [SubtypeDefault(typeof(UnknownFrame))]
    public Frame Frame { get; set; }
}
```

```c#
public class FrameFactory : ISubtypeFactory
{
    public bool TryGetKey(Type valueType, out object key)
    {
        if (valueType == typeof(UpdateFrame))
        {
            key = FrameType.Update;
        }
        else if (valueType == typeof(PingFrame))
        {
            key = FrameType.Ping;
        }
        else
        {
            key = null;
            return false;
        }

        return true;
    }

    public bool TryGetType(object key, out Type type)
    {
        switch (Convert.ToInt32(key))
        {
            case FrameType.Update:
            {
                type = typeof(UpdateFrame);
                break;
            }
            case FrameType.Ping:
            {
                type = typeof(PingFrame);
                break;
            }
            default:
            {
                type = null;
                return false;
            }
        }

        return true;
    }
}
```

Additionally, the SubtypeDefault attribute may be used to specify a fallback subtype to be used in the event that an unknown indicator is encountered during deserialization.  During serialization the default subtype may be included without a corresponding subtype binding.  In this case the source must be set correctly prior to serialization.

```c#
public class ChunkContainer
{
    [FieldOrder(0)]
    [SerializeAs(Endianness = Endianness.Big)]
    public int Length { get; set; }

    [FieldOrder(1)]
    [FieldLength(4)]
    public string ChunkType { get; set; }

    [FieldOrder(2)]
    [FieldLength("Length")]
    [Subtype("ChunkType", "IHDR", typeof(ImageHeaderChunk))]
    [Subtype("ChunkType", "PLTE", typeof(PaletteChunk))]
    [Subtype("ChunkType", "IDAT", typeof(ImageDataChunk))]
    // etc
    public Chunk Chunk { get; set; }

    [FieldOrder(3)]
    [SerializeAs(Endianness = Endianness.Big)]
    public int Crc { get; set; }
}
```

```c#
List<ChunkContainer> Chunks { get; set; }
```

Note that the Chunk field is bound to both the Length field and the ChunkType field.  If the serializer can resolve a known chunk type, it will instantiate and deserialize it.  However, if it encounters an unknown value in the ChunkType field it is still able to skip past it using the Length binding.  Also note that the CRC is included for completeness but will not be updated by the framework during serialization nor checked during deserialization.


### SerializeAsAttribute ###

In general you shouldn't need this as most things tend to work out without it.  However, you can always override the default behavior by specifying SerializeAs.

### SerializeAsEnumAttribute ###

The SerializeAsEnum attribute allows you specify an alternate value for an enum to be used during the operation.

```c#
public enum Waypoints
{
    [SerializeAsEnum("Alpha")]
    A,
    [SerializeAsEnum("Bravo")]
    B,
    [SerializeAsEnum("Charlie")]
    C
}
```


### SerializeWhenAttribute ###

The SerializeWhen attribute can be used to conditionally serialize or deserialize a field based on bound predicate.  If multiple SerializeWhen attributes are specified they will be or'd together.

```c#
[SerializeWhen("Version", HardwareVersion.XBeeSeries1)]
[SerializeWhen("Version", HardwareVersion.XBeeProSeries1)]
public ReceivedSignalStrengthIndicator RSSI { get; set; }
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/WhenBinding.png" />
</p>

### SerializeWhenNotAttribute ###

Identitcal to the SerializeWhen attribute for negative conditions. 

### SerializeUntilAttribute ###

The SerializedUntil attribute can be used to terminate a collection once a specified value is encountered, essentially allowing for the creation of "null-terminated" lists or the like.  This attribute is not currently supported when deserializing from non-seekable streams.

```c#
[SerializeUntil((byte)0)]
public List<DirectoryRecord> Records { get; set; }
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/Until.png" />
</p>

Note that a significant disadvantage of this approach is that the DirectoryRecord object cannot start with a null!  In general, you should avoid this approach when defining formats.  However, in some cases you may not have a choice (this exact construct appears in the ISO 9660 specification).

### ItemLengthAttribute ###

This attribute can be used to control the length of items in a collection.

```c#
public class Manifest
{
    [FieldOrder(0)]
    public byte EntryLength { get; set; }

    [FieldOrder(1)]
    [ItemLength("EntryLength")]
    public List<string> Entries { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/ItemLengthBinding.png" />
</p>

In this case, the collection deserialization terminates correctly if this is the only thing in the object graph.  However, if this is part of a larger graph, we might need to also declare a count.

```c#
public class Manifest
{
    [FieldOrder(0)]
    public byte EntryCount { get; set; }

    [FieldOrder(1)]
    public byte EntryLength { get; set; }

    [FieldOrder(2)]
    [FieldCount("EntryCount")]
    [ItemLength("EntryLength")]
    public List<string> Entries { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/ItemLengthAndCountBinding.png" />
</p>

Lastly, ItemLength can be used to form jagged arrays of values by binding to sources that implement IEnumerable such as in the following example.

```c#
public class JaggedArrayClass
{
    [FieldOrder(0)]
    public int NameCount { get; set; }

    [FieldOrder(1)]
    [FieldCount("NameCount")]
    public int[] NameLengths { get; set; }

    [FieldOrder(2)]
    [FieldCount("NameCount")]
    [ItemLength("NameLengths")]
    public string[] Names { get; set; }
}
```

Note that the ordering of the values and value lengths must coincide for this approach to work.


### ItemSubtypeAttribute ###

The ItemSubtype attribute is similar to the Subtype attribute but can be used to specify an item subtype on homogenous collections.

```c#
public class ChocolateBox
{
	[FieldOrder(0)]
	public ChocolateType Type { get; set; }

	[FieldOrder(1)]
	[ItemSubtype("Type", ChocolateType.Dark, typeof(DarkChocolate))]
	[ItemSubtype("Type", ChocolateType.NutsAndChews, typeof(NutsAndChewsChocolate))]
	public List<Chocolate> Chocolates;
}
```

### ItemSerializeUntilAttribute ###

The ItemSerializeUntil attribute can be used to terminate a collection when an item with a specified value is encountered.  This is similar to the SerializeUntil attribute, except the ItemSerializeUntil attribute will first attempt to deserialize a valid item and then check the equality, whereas SerializeUntil will first check the equality and then only deserialize the result if the collection has not terminated.

```c#
public class Toy
{
    public string Name { get; set; }
    public bool IsLast { get; set; }
}

public class ToyChest
{
    [ItemSerializeUntil("IsLast", true)]
    public List<Toy> Toys { get; set; }
}
```

<p align="center">
  <img src="https://github.com/jefffhaynes/BinarySerializer/blob/master/BinarySerializer.Docs/ItemUntil.png" />
</p>

This attribute can be used to break many blocks into multiple sections using the LastItemMode property.

```c#
public class Section
{
    [FieldOrder(0)]
    public Block Header { get; set; }
	
    [FieldOrder(1)]
    [ItemSerializeUntil("Type", BlockType.Header, LastItemMode = LastItemMode.Defer)]
    public List<Block> Blocks { get; set; }
}

public class Document
{
    public List<Section> Sections { get; set; }
}
```

This will deserialize blocks until a block with type Header is encountered.  At that point, because LastItemMode is set to Defer, the stream will be rewound and used to deserialize the next section.

-----------

### Performance ###

Serialization and deserialization operations are broken into four phases.

* Reflection
* Value assignment
* Binding
* Serialization/Deserialization

During the first and most expensive stage reflection information is cached in memory for every type that the serializer encounters.  Once a type is encountered it is stored statically across all instances.  This approach reduces complexity and improves performance when compared to disk caching; however, it should be noted that this behavior also increase the overall memory footprint.

When serializing an object graph it is possible to defer type resolution by simply specifying object or an abstract type for which no corresponding SubtypeAttributes exist.  The serializer will defer reflection in these cases but will not store the results.  As such, it is best to avoid this approach and specify type information in a way that can be resolved prior to serialization, either explicitly or by using the SubtypeAttribute.  This approach is also desirable as it guarantees the serializer can deserialize serialized data.

### Enums ###
Enums can be used to create expressive definitions.  Depending on what attributes are specified enums will be interpreted by the serializer as either the underlying value, the literal value of the enum, or a value specified with the SerializeAsEnum attribute.  In the following example, the field will be serialized as the underlying byte.

```c#
public enum Shape : byte
{
    Circle = 0x0,
    Square = 0x1
}

public class EnumClass
{
    public Shape Shape { get; set; }
}
```

Serializing this class would result in a single byte.  Alternatively, you may want the name of the enum to be serialized:

```c#
public class EnumClass
{
    [SerializeAs(SerializedType.NullTerminatedString)]
    public Shape Shape { get; set; }
}
```

You could also specify this to be a fixed-sized string, etc.

### Streams ###

In some cases you may be serializing or deserializing large amounts of data, which is logically broken into blocks or volumes.  In these cases it may be advantageous to defer handling of those sections, rather than dealing with large in-memory buffers.

```c#
[FieldOrder(22)]
[SerializeWhen("RecordType", RecordType.File)]
[FieldOffset("FirstSectorData", ConverterType = typeof(SectorByteConverter))]
[FieldLength("DataLength")]
public Stream Data { get; set; }
```

In this example, the Data stream will be copied from the source stream during serialization.  However, on deserialization, the resulting object graph will contain a Streamlet object, which references a section of the source stream and allows for deferred read access.  Note that this feature is only supported when the underlying source stream supports seeking.  When dealing with non-seekable streams (e.g. NetworkStream), it is better to deserialize the stream in frames or packets where possible rather than try to deserialize the entire stream (which in some cases may be open-ended) at once.


### Nulls ###

Null values are allowed; however, bear in mind that this can lead to unstable definitions.  While serialization may work, deserialization may fail if the framework is unable to deduce which fields are meant to be null and which are not.

### Nullable types ###

Nullable types are supported and are serialized, if present, as the underlying type.

### Advanced Binding ###

Binding is not limited to fields in the same object but can be used to reference arbitrary fields accessible throughout the graph.  Ancestors in the graph can be located by either type or level and can be used as references for binding.

```c#
public class Container
{
    [FieldOrder(0)]
    public byte NameLength { get; set; }

    [FieldOrder(1)]
    public Person Person { get; set; }
}

public class Person
{
    [FieldOrder(0)]
    [FieldLength("NameLength", Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(Container))]
    public string Name1 { get; set; }

    // is equivalent to

    [FieldOrder(1)]
    [FieldLength("NameLength", Mode = RelativeSourceMode.FindAncestor, AncestorLevel = 2)]
    public string Name2 { get; set; }
}
```

#### Value Converter ####

Sometimes binding directly to a source is insufficient and in those cases your best option is to define a value converter, which can be specified as part of the binding.

```c#
class SectorByteConverter : IValueConverter
{
    public object Convert(object value, object converterParameter, BinarySerializationContext context)
    {
        var sector = (uint) value;
        var iso = context.FindAncestor<Iso9660>();
        var sectorSize = iso.PrimaryVolumeDescriptor.SectorSize;
        return sector*sectorSize;
    }

    public object ConvertBack(object value, object converterParameter, BinarySerializationContext context)
    {
        // etc
    }
}
```

```c#
[FieldOffset("FirstSector", ConverterType = typeof(SectorByteConverter))]
[SerializeUntil((byte)0)]
public List<DirectoryRecord> Records { get; set; }
```

### Custom Serialization ###

When all else fails, you can define a custom serialization object.

```c#
/// <summary>
/// A custom serializer for variable byte representation of an integer.
/// </summary>
public class Varuint : IBinarySerializable
{
    [Ignore]
    public uint Value { get; set; }

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        bool more = true;
        int shift = 0;
        Value = 0;

        while (more)
        {
            int b = stream.ReadByte();

            if (b == -1)
                throw new InvalidOperationException("Reached end of stream before end of varuint.");

            var lower7Bits = (byte)b;
            more = (lower7Bits & 128) != 0;
            Value |= (uint)((lower7Bits & 127) << shift);
            shift += 7;
        }
    }


    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        var value = Value;
        do
        {
            var lower7Bits = (byte) (value & 127);
            value >>= 7;
            if (value > 0)
                lower7Bits |= 128;
            stream.WriteByte(lower7Bits);
        } while (value > 0);
    }
}
```
Note that when using custom serialization the object can still be used as a source for binding.  In the above example you could bind to "Value" from elsewhere in your object hierarchy.

Also note that in the case that FieldLength is specified for the custom object, the stream passed in during serialization will be limited to that length.  This can help if you want to copy the entirety or balance of the object stream into a member field.  In essence, the custom object is unable to stray from the bounds set for it by the serialization object hierarchy.

### Encoding ###

Text encoding can be specified by the <code>FieldEncodingAttribute</code> and is inherited by all children unless overridden.

```c#
[FieldEncoding("windows-1256")]
public string Name { get; set;  }
```

### Endianness ###

Maybe a quaint topic these days but incredibly painful if you're suddenly faced with it.  BinarySerializer handles endianness in two ways: globally or on a field basis.  If you're working in a system that deals entirely in big endian, you can simply write:

```c#
serializer.Endianness = Endianness.Big;
```
 
In other cases you may actually have a mix of big and little endian and again you can use the <code>FieldEndiannessAttribute</code> to specify endianness.  As with encoding, endianness is inherited through the graph hierarchy unless overridden by a child field.

```c#
[FieldEndianness(Endianness.Big)]
public uint SectorCountBig { get; set; }
```

### Immutable Types ###

In certain cases you may not want to expose public setters on deserialized objects.  BinarySerializer will look for constructors with matching property or field parameter names and favor those over the default constructor.  In cases where multiple constructors are defined, the serializer will look for a best fit.

```c#
public class LongAddress
{
    public LongAddress(int low, int high)
    {
        Low = low;
        High = high;
    }

    [FieldOrder(0)]
    public int Low { get; private set; }

    [FieldOrder(1)]
    public int High { get; private set; }
}
```

### Debugging ###

To gain insight into the serialization or deserialization process the serializer defines a number of events.  These events can be useful when attempting to troubleshoot if the debugger is insufficient (a common problem in declarative frameworks).

Here is an example implementation of basic tracing for the serialization and deserialization process.

```c#
var serializer = new BinarySerializer();

serializer.MemberSerializing += OnMemberSerializing;
serializer.MemberSerialized += OnMemberSerialized;
serializer.MemberDeserializing += OnMemberDeserializing;
serializer.MemberDeserialized += OnMemberDeserialized;


private static void OnMemberSerializing(object sender, MemberSerializingEventArgs e)
{
    Console.CursorLeft = e.Context.Depth * 4;
    Console.WriteLine("S-Start: {0} @ {1}", e.MemberName, e.Offset);
}

private static void OnMemberSerialized(object sender, MemberSerializedEventArgs e)
{
    Console.CursorLeft = e.Context.Depth * 4;
    var value = e.Value ?? "null";
    Console.WriteLine("S-End: {0} ({1}) @ {2}", e.MemberName, value, e.Offset);
}

private static void OnMemberDeserializing(object sender, MemberSerializingEventArgs e)
{
    Console.CursorLeft = e.Context.Depth * 4;
    Console.WriteLine("D-Start: {0} @ {1}", e.MemberName, e.Offset);
}

private static void OnMemberDeserialized(object sender, MemberSerializedEventArgs e)
{
    Console.CursorLeft = e.Context.Depth * 4;
    var value = e.Value ?? "null";
    Console.WriteLine("D-End: {0} ({1}) @ {2}", e.MemberName, value, e.Offset);
}
```

### Extending FieldValue Attributes ###

FieldValue attributes can be used to perform complex calculations based on field values.  The following is an example of a FieldValue attribute that performs a cryptographic hash.

```c#
public class FieldSha256Attribute : FieldValueAttributeBase
{
    private readonly SHA256Managed _sha = new SHA256Managed();

    public override int BlockSize => _sha.InputBlockSize;

    public FieldSha256Attribute(string valuePath) : base(valuePath)
    {
    }

    protected override void Reset(BinarySerializationContext context)
    {
        _sha.Initialize();
    }

    protected override void Compute(byte[] buffer, int offset, int count)
    {
        _sha.TransformBlock(buffer, offset, count, buffer, offset);
    }

    protected override object ComputeFinal()
    {
        _sha.TransformFinalBlock(new byte[0], 0, 0);
        return _sha.Hash;
    }
}

public class FieldSha256Class
{
    [FieldOrder(0)]
    public int Length { get; set; }

    [FieldOrder(1)]
    [FieldLength("Length")]
    [FieldSha256("Hash")]
    public string Value { get; set; }

    [FieldOrder(2)]
    public byte[] Hash { get; set; }
}
```

### Exceptions ###

If an exception does occur either during the initial reflection phase or subsequent serialization, every layer of the object graph will throw its own exception, keeping the prior exception as the inner exception.  Always check the inner exception for more details.

### Thread Safety ###

All public members of BinarySerializer are thread-safe and may be used concurrently from multiple threads.

## Examples ##

[XBee](https://github.com/jefffhaynes/XBee) 

[PcapNet](https://github.com/jefffhaynes/PcapNet) 

[PcapNgNet](https://github.com/jefffhaynes/PcapNgNet) 
