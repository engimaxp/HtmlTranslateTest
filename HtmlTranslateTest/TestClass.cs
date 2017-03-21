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
            Assert.AreEqual(rootTag.ToString(), htmltag);
            // TODO: Add your test code here
            Assert.Pass("Your first passing test");
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
    }
}
