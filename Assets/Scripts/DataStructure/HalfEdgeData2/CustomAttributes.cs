using System;

namespace DataStructure
{
    public class CustomAttributes
    {
        public class JsonIgnoreAttribute : Attribute
        {
        }

        internal class JsonPropertyAttribute : Attribute
        {
            public JsonPropertyAttribute(string propertyName)
            {
            
            }
        }
    }
}