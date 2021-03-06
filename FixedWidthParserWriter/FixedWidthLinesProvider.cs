﻿using System;
using System.Collections.Generic;
using System.Linq;
using FastMember;

namespace FixedWidthParserWriter
{
    public class FixedWidthLinesProvider<T> : FixedWidthBaseProvider where T : class, new()
    {
        public T Parse(string line, int structureTypeId = 0)
        {
            return Parse(new List<string>() { line }, structureTypeId)[0];
        }

        public List<T> Parse(List<string> lines, int structureTypeId = 0)
        {
            StructureTypeId = structureTypeId;
            List<T> result = new List<T>();
            foreach (var line in lines)
            {
                result.Add(ParseData<T>(new List<string> { line }, FieldType.LineField));
            }
            return result;
        }
        public string Write(T data, int structureTypeId = 0)
        {
            return Write(new List<T>() { data }, structureTypeId)[0];
        }

        public List<string> Write(List<T> data, int structureTypeId = 0)
        {
            StructureTypeId = structureTypeId;
            char recordType = new char();

            LoadNewDefaultConfig(new T());

            var accessor = TypeAccessor.Create(typeof(T), true);
            var memberSet = accessor.GetMembers().Where(a => a.IsDefined(typeof(FixedWidthLineFieldAttribute)));
            var membersDict = new Dictionary<int, Member>();
            var attributesDict = new Dictionary<string, FixedWidthLineFieldAttribute>();
            var memberNameTypeNameDict = new Dictionary<string, string>();

            foreach (var classAttribute in System.Attribute.GetCustomAttributes(typeof(T)))
            {
                var fixedWidthAttribute = classAttribute as FixedWidthAttribute;
                if (fixedWidthAttribute != null)
                    if (fixedWidthAttribute.StructureTypeId == StructureTypeId)
                        {
                            recordType = fixedWidthAttribute.RecordType;
                            break;
                        }
            }

            foreach (var member in memberSet)
            {
                var attribute = member.GetMemberAttributes<FixedWidthLineFieldAttribute>().SingleOrDefault(a => a.StructureTypeId == StructureTypeId);
                if (attribute != null)
                {
                    membersDict.Add(attribute.Start, member);
                    attributesDict.Add(member.Name, attribute);
                    memberNameTypeNameDict.Add(member.Name, member.Type.Name);
                }
            }
            
            var membersData = membersDict.OrderBy(a => a.Key).Select(a => a.Value);

            List<string> resultLines = new List<string>();
            foreach (T element in data)
            {
                string line = String.Empty;

                int startPrev = 1;
                int lengthPrev = 0;

                if (recordType != new char())
                {
                    line += recordType;
                    lengthPrev = 1;
                }

                foreach (var propertyMember in membersData)
                {
                    var attribute = attributesDict[propertyMember.Name];
                    if (startPrev + lengthPrev != attribute.Start)
                        throw new InvalidOperationException($"Invalid Start or Length parameter, {attribute.Start} !=  {startPrev + lengthPrev}" +
                                                            $", on FixedLineFieldAttribute (property {propertyMember.Name}) for StructureTypeId {StructureTypeId}");
                    startPrev = attribute.Start;
                    lengthPrev = attribute.Length;

                    var memberData = new FastMemberData()
                    {
                        Member = propertyMember,
                        Accessor = accessor,
                        Attribute = attribute,
                        MemberNameTypeNameDict = memberNameTypeNameDict
                    };
                    line += WriteData(element, memberData, FieldType.LineField);
                }
                resultLines.Add(line);
            }
            return resultLines;
        }
    }
}
