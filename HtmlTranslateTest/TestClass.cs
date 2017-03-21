using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTranslateTest
{
    [TestFixture]
    public class TestClass
    {
        [Test]
        public void TestMethod()
        {
            var htmltag = "<p class=\"class1\">hello<a href='http://www.baidu.com'><img src=\"randomimg.png\" /> </a> &nbsp;you're welcomed </p><br/>";
            var tag1 = HtmlTag.Create("src=\"randomimg.png\"", TagType.Image);
            var tag2 = HtmlTag.Create("href='http://www.baidu.com'", TagType.Default);
            var tag3 = HtmlTag.Create(null, TagType.Default, "hello");
            var tag4 = HtmlTag.Create(null, TagType.Default, " you're welcomed ");
            var tag5 = HtmlTag.Create(null, TagType.Breaking);
            var tag6 = HtmlTag.Create(null, TagType.Default, " ");
            var tag7 = HtmlTag.Create(null, TagType.Breaking);
            tag2.AddChild(tag1);
            tag2.AddChild(tag6);
            tag7.AddChild(tag3);
            tag7.AddChild(tag2);
            tag7.AddChild(tag4);
            var rootTag = HtmlTag.Create(null, TagType.Breaking);
            rootTag.AddChild(tag7);
            rootTag.AddChild(tag5);
            Assert.AreEqual(rootTag, DecodeHtmlTag(htmltag));
            // TODO: Add your test code here
            Assert.Pass("Your first passing test");
        }

        private object DecodeHtmlTag(string htmltag)
        {
            throw new NotImplementedException();
        }
    }

    public enum TagType
    {
        Breaking,
        Image,
        Default
    }

    public class HtmlTag
    {
        public static HtmlTag Create(string properties, TagType type, string content = null)
        {
            return new HtmlTag()
            {
                type = type,
                properties = properties,
                content = content
            };
        }

        public TagType type { get; set; }
        public string properties { get; set; }
        public string content { get; set; }
        private readonly List<HtmlTag> ChildTags = new List<HtmlTag>();
        private string link
        {
            get { return properties; }
        }

        public void AddChild(HtmlTag child)
        {
            ChildTags.Add(child);
        }

        public override string ToString()
        {
            StringBuilder resultBuilder = new StringBuilder();
            foreach (var tag in ChildTags)
            {
                resultBuilder.Append(tag);
            }
            return resultBuilder.ToString();
        }

        protected bool Equals(HtmlTag other)
        {
            bool result = type == other.type && string.Equals(properties, other.properties) && string.Equals(content, other.content);
            if(result == false) return result;
            if (ChildTags == null && other.ChildTags == null) return true;
            if (ChildTags!=null && other.ChildTags!=null)
            {
                if (ChildTags.Count == 0 && other.ChildTags.Count == 0) return true;
                if (ChildTags.Count != other.ChildTags.Count) return false;
                return !ChildTags.Where((t, i) => !t.Equals(other.ChildTags[i])).Any();
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HtmlTag) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) type;
                hashCode = (hashCode*397) ^ (properties != null ? properties.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (content != null ? content.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
