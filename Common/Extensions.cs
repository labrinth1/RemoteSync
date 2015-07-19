using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    public static class Extensions
    {
        public static byte[] ObjectToByteArray(this Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static Object ByteArrayToObject(this byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

        public static T Cast<T>(this Object myobj)
        {

            Type objectType = myobj.GetType();
            Type target = typeof(T);

            var z = from source in objectType.GetMembers().ToList() where source.MemberType == MemberTypes.Property select source;
            var d = from source in target.GetMembers().ToList() where source.MemberType == MemberTypes.Property select source;
            List<MemberInfo> members = d.Where(memberInfo => d.Select(c => c.Name).ToList().Contains(memberInfo.Name)).ToList();
            PropertyInfo propertyInfo;
            object value;
            var x = Activator.CreateInstance(target, "", false);

            foreach (var memberInfo in members)
            {
                if (memberInfo.Name != "Service" && memberInfo.Name != "FolderPath")
                {
                    propertyInfo = typeof(T).BaseType.GetProperty(memberInfo.Name);
                    value = myobj.GetType().GetProperty(memberInfo.Name).GetValue(myobj, null);
                    propertyInfo.SetValue(x, value, null);
                }
            }
            return (T)x;
        }
    }
}
