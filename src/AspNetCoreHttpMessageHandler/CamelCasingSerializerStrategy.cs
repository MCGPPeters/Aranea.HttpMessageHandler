namespace AspNetCoreHttpMessageHandler
{
    public class CamelCasingSerializerStrategy : PocoJsonSerializerStrategy
    {
        protected override string MapClrMemberNameToJsonFieldName(string clrPropertyName)
        {
            return clrPropertyName.ToCamelCase();
        }
    }
}