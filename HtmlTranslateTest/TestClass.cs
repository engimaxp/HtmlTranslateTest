using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HtmlTranslateTest
{
    [TestFixture]
    public class TestClass
    {
        [Test]
        [Ignore("Ignore For Now")]
        public void TestMethod()
        {
            var htmltag = "<p class=\"class1\">hello<a href='http://www.baidu.com'><img src=\"randomimg.png\" /> </a> &nbsp;you're welcomed </p><br/>";
            var tag1 = HtmlNode.Create("src=\"randomimg.png\"", TagType.Image);
            var tag2 = HtmlNode.Create("href='http://www.baidu.com'", TagType.Default);
            var tag3 = HtmlNode.Create(null, TagType.Default, "hello");
            var tag4 = HtmlNode.Create(null, TagType.Default, " you're welcomed ");
            var tag5 = HtmlNode.Create(null, TagType.Breaking);
            var tag6 = HtmlNode.Create(null, TagType.Default, " ");
            var tag7 = HtmlNode.Create(null, TagType.Breaking);
            tag2.AddChild(tag1);
            tag2.AddChild(tag6);
            tag7.AddChild(tag3);
            tag7.AddChild(tag2);
            tag7.AddChild(tag4);
            var rootTag = HtmlNode.Create(null, TagType.Breaking);
            rootTag.AddChild(tag7);
            rootTag.AddChild(tag5);
            Assert.AreEqual(rootTag, DecodeHtmlTag(htmltag));
            // TODO: Add your test code here
            Assert.Pass("Your first passing test");
        }

        [Test]
        public void TestNullHtmlTag()
        {
            Assert.AreEqual(HtmlNode.CreateRoot(),DecodeHtmlTag(""));
        }
        [Test]
        public void TestPlainHtmlTag()
        {
            string mock = "Hello";
            var root = HtmlNode.CreateRoot();
            root.AddChild(HtmlNode.CreatePlainTag(mock));
            Assert.AreEqual(root, DecodeHtmlTag(mock));
        }
        [Test]
        public void TestSingleHtmlTag()
        {
            string mock = "<br/>Hello";
            var root = HtmlNode.CreateRoot(); 
            root.AddChild(HtmlNode.Create(null, TagType.Breaking));
            root.AddChild(HtmlNode.CreatePlainTag("Hello"));
            Assert.AreEqual(root.ToString(), DecodeHtmlTag(mock).ToString());
        }

        [Test]
        public void TestTwiceHtmlTag()
        {
            string mock = "<br/><p>1</p>";
            var root = HtmlNode.CreateRoot();
            root.AddChild(HtmlNode.Create(null, TagType.Breaking));
            root.AddChild(HtmlNode.Create(null, TagType.Breaking,"1"));
            Assert.AreEqual(root.ToString(), DecodeHtmlTag(mock).ToString());
        }
        [Test]
        public void TestNestHtmlTag()
        {
            string mock = "<p>1<br/></p>";
            var root = HtmlNode.CreateRoot();
            var br = HtmlNode.Create(null, TagType.Breaking);
            var plain = HtmlNode.CreatePlainTag("1");
            var p = HtmlNode.Create(null, TagType.Breaking);
            p.AddChild(plain);
            p.AddChild(br);
            root.AddChild(p);
            Assert.AreEqual(root.ToString(), DecodeHtmlTag(mock).ToString());
        }

        [Test]
        public void TestComplexHtmlTag()
        {
            string mock = "<a target=\"_blank\" href=\"test\"><img src=\"test\"/></a><p>1<br/></p>";
            var root = HtmlNode.CreateRoot();
            var link = HtmlNode.Create("target=\"_blank\" href=\"test\"", TagType.Default);
            var img = HtmlNode.Create("src=\"test\"", TagType.Image);
            link.AddChild(img);
            var p = HtmlNode.Create(null, TagType.Breaking);
            var plain = HtmlNode.CreatePlainTag("1");
            p.AddChild(plain);
            p.AddChild(HtmlNode.Create(null,TagType.Breaking));
            root.AddChild(link);
            root.AddChild(p);
            Assert.AreEqual(root.ToString(), DecodeHtmlTag(mock).ToString());
        }

        [Test]
        public void TestTranscodeToJsonObj()
        {
            string mock = "<a target=\"_blank\" href=\"test\"><img src=\"test\"/></a><p>1<br/></p>";
            var list = new List<ClientContentItem>();
            list.Add(new ClientContentItem()
            {
                TextType = ClientTextType.Photo,
                Link = "target=\"_blank\" href=\"test\"",
                Text = "src=\"test\""
            });
            list.Add(new ClientContentItem()
            {
                TextType = ClientTextType.Text,
                Text = "1\r\n\r\n"
            });
            var actual = TransCodeHtmlToList(mock);
            var expect = JsonConvert.SerializeObject(list);
            Assert.AreEqual(expect, actual);
        }

        [Test]
        public void TestComplexTranscodeToJsonObj()
        {
            string mock = "<p></p>";
            var result = TransCodeHtmlToList(mock);
            Assert.IsNotNull(result);
        }
        private string TransCodeHtmlToList(string html)
        {
            var mockHtmlTag = DecodeHtmlTag(html);
            ClientContentItem cci = new ClientContentItem();
            List<ClientContentItem> returnList = new List<ClientContentItem>();
            mockHtmlTag.TransCodeToList(ref cci, ref returnList, null);
            if (!cci.isEmpty())
                returnList.Add(cci);
            return JsonConvert.SerializeObject(returnList);
        }

        private HtmlNode DecodeHtmlTag(string html)
        {
            
            HtmlNode root = HtmlNode.CreateRoot();
            //无内容
            if (string.IsNullOrEmpty(html))
                return root;

            var doms = ConvertTagToList(html);
            //无html标签
            if (doms.Count == 0)
            {
                root.AddChild(HtmlNode.CreatePlainTag(html));
                return root;
            }
            //再以此处理各个位置的关系
            root.AddChilds(CreateNodesFromTag(doms,0,html,0));
            return root;
        }

        private static TagType NameToTagType(string tagname) 
        {
            if(string.IsNullOrEmpty(tagname)) return TagType.Default;
            switch (tagname.ToLower().Trim())
            {
                case "p":
                case "br":
                {
                    return TagType.Breaking;
                }
                case "img":
                {
                    return TagType.Image;
                }
                default:
                    return TagType.Default;
            }
        }

        private static List<HtmlNode> CreateNodesFromTag(List<HtmlTag> tags, int targetStartPos,string html, int tagLevel)
        {
            if (string.IsNullOrEmpty(html)) return null;
            List<HtmlNode> result = new List<HtmlNode>();
            int currentPos = 0;
            var currentProcessingTag = tags.Where(x => x.tagLevel == tagLevel).ToList();
            foreach (var tag in currentProcessingTag)
            {
                result.Add(HtmlNode.CreatePlainTag(html.Substring(currentPos, tag.tagStartPos - currentPos - targetStartPos)));
                var currentNode = HtmlNode.Create(tag.properties, NameToTagType(tag.tagname));
                currentNode.AddChilds(CreateNodesFromTag(tags.Where(r => r.tagStartPos <= tag.tagContentEndPos && r.tagStartPos >= tag.tagContentStartPos).ToList(), tag.tagContentStartPos, html.Substring(tag.tagContentStartPos - targetStartPos, tag.tagContentEndPos - tag.tagContentStartPos), tagLevel + 1));
                result.Add(currentNode);
                currentPos = tag.tagEndPos - targetStartPos;
            }
            result.Add(HtmlNode.CreatePlainTag(html.Substring(currentPos,html.Length-currentPos)));
            return result;
        }

        private static List<HtmlTag> ConvertTagToList(string html)
        {
            Regex TagPattern = new Regex(@"(?m)(?:<\s*(?:\w+)\s*)|(?:(?:<\s*/\w*)?[/]?\s*>)");//匹配html标签头尾
            MatchCollection mc = TagPattern.Matches(html);
            List<HtmlTag> doms = new List<HtmlTag>();
            if (mc.Count == 0)
            {
                return doms;
            }
            //首先尝试使用堆栈来建立一个位置列表，将包含关系先确立
            //tagname,tagStartPos,tagContentStartPos,tagProperties
            Stack<Tuple<string, int, int, string>> tagStack = new Stack<Tuple<string, int, int, string>>();
            int curPosition = 0;
            Regex startTagPattern = new Regex(@"<\s*(\w+)\s*");
            Regex propertyEndPattern = new Regex(@"^\s*>$");
            Regex propertyEndAndTagEndPattern = new Regex(@"^\s*/\s*>$");
            foreach (Match match in mc)
            {
                if (startTagPattern.IsMatch(match.Value)) //入栈
                {
                    curPosition = match.Index + match.Length;
                    tagStack.Push(
                        new Tuple<string, int, int, string>(startTagPattern.Match(match.Value).Groups[1].Captures[0].Value,
                            match.Index, curPosition, string.Empty));
                }
                else if (propertyEndPattern.IsMatch(match.Value))
                {
                    var properties = html.Substring(curPosition, match.Index - curPosition);
                    var tag = tagStack.Pop();
                    curPosition = match.Index + match.Length;
                    tagStack.Push(new Tuple<string, int, int, string>(tag.Item1, tag.Item2, curPosition, properties));
                }
                else //出栈
                {
                    var tag = tagStack.Pop();
                    var tagContentStartPos = tag.Item3;
                    string properties = tag.Item4;
                    if (string.IsNullOrEmpty(properties) && propertyEndAndTagEndPattern.IsMatch(match.Value))
                    {
                        properties = html.Substring(curPosition, match.Index - curPosition);
                        curPosition = match.Index;
                        tagContentStartPos = curPosition;
                    }
                    doms.Add(new HtmlTag()
                    {
                        tagname = tag.Item1,
                        tagStartPos = tag.Item2,
                        tagContentStartPos = tagContentStartPos,
                        tagContentEndPos = match.Index,
                        tagEndPos = match.Index + match.Length,
                        tagLevel = tagStack.Count,
                        properties = properties
                    });
                }
            }
            return doms;
        }
    }

    public class HtmlTag
    {
        public string properties { get; set; }
        public int tagStartPos { get; set; }
        public int tagContentStartPos { get; set; }
        public int tagContentEndPos { get; set; }
        public int tagEndPos { get; set; }
        public string tagname { get; set; }
        public int tagLevel { get; set; }

        public override string ToString()
        {
            return string.Format("{{{0}, {1}, {2}, {3}, {4}, {5}, {6}}}", properties, tagStartPos, tagContentStartPos, tagContentEndPos, tagEndPos, tagname, tagLevel);
        }
    }

    public enum TagType
    {
        Breaking,
        Image,
        Default
    }

    public class HtmlNode
    {
        public static string ConvertUrl(string url, string type)
        {
            if (url.StartsWith("/webapp/"))
            {
                if (type.Trim() == "2")
                {
                    return "m.ctrip.com/"+url; //ConfigurationManager.AppSettings["m.ctrip.com"] + url;
                }
                else if (type.Trim() == "4")
                {
                    if (url.StartsWith("/webapp/myctrip/"))
                    {
                        url = "myctrip/index.html#" + url;
                    }
                    else
                    {
                        url = url.Replace("/webapp/", "");
                    }
                    return "ctrip://wireless/h5?url=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url)) + "&type=1";
                }
                else
                {
                    return url;
                }
            }
            else
            {
                return url;
            }

        }
        private static readonly Regex regexFun = new Regex(@"(?i)\bLizard.jump\b\('(?<url>.*)\',{\btargetModel\b:(?<type>.*)}\)");
        private static readonly Regex REGEX_URL = new Regex(@"(?i)((http|https)://)?(www.)?(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,9})*(/[a-zA-Z0-9\&#,%_\./-~-]*)?");
        private static readonly Regex REGEX_PICTURE_URL = new Regex(@"(?i)src=['""](\S+)['""]\s");
        public static HtmlNode Create(string properties, TagType type, string content = null)
        {
            return new HtmlNode()
            {
                type = type,
                properties = properties,
                content = content
            };
        }
        public static HtmlNode CreateRoot()
        {
            return Create(null, TagType.Default);
        }
        public static HtmlNode CreatePlainTag(string text)
        {
            return Create(null, TagType.Default, text);
        }

        public TagType type { get; set; }
        public string properties { get; set; }
        public string content { get; set; }
        private readonly List<HtmlNode> ChildTags = new List<HtmlNode>();
        public string link
        {
            get
            {
                if (string.IsNullOrEmpty(properties)) return string.Empty;
                MatchCollection mcFun = regexFun.Matches(properties);
                if (mcFun.Count > 0)
                {
                    return ConvertUrl(mcFun[0].Groups["url"].Value, mcFun[0].Groups["type"].Value);
                }
                mcFun = REGEX_URL.Matches(properties);
                if (mcFun.Count > 0)
                {
                    return mcFun[0].Groups[0].Captures[0].Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public string src
        {
            get
            {
                if (string.IsNullOrEmpty(properties)) return string.Empty;
                MatchCollection mcFun = regexFun.Matches(properties);
                mcFun = REGEX_PICTURE_URL.Matches(properties);
                if (mcFun.Count > 0)
                {
                    return mcFun[0].Groups[1].Captures[0].Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public void AddChild(HtmlNode child)
        {
            if (child == null) return;
            if (child.type == TagType.Default && string.IsNullOrEmpty(child.content) &&
                string.IsNullOrEmpty(child.properties))
                return;
            ChildTags.Add(child);
        }
        public void AddChilds(List<HtmlNode> childs)
        {
            if (childs == null || childs.Count == 0) return;
            if (childs.TrueForAll(x => x.IsPlainText()))
            {
                if (content == null) content = String.Empty;
                childs.ForEach(r=>content+=r.content);
            }
            else
            {
                childs.ForEach(AddChild);    
            }
        }

        public bool IsPlainText()
        {
            if (type == TagType.Default && string.IsNullOrEmpty(properties))
            {
                if (ChildTags == null || ChildTags.Count == 0) return true;
                return ChildTags.TrueForAll(r => r.IsPlainText());
            }
            return false;
        }

        public override string ToString()
        {
            return FormatIndexString(String.Empty);
        }

        public void TransCodeToList(ref ClientContentItem currentItem, ref List<ClientContentItem> returnList,string olink)
        {
            if (type != TagType.Image)
            {
                currentItem.Text += content;
            }
            if(returnList == null) returnList = new List<ClientContentItem>();
            foreach (HtmlNode t in ChildTags)
            {
                t.TransCodeToList(ref currentItem, ref returnList, string.IsNullOrEmpty(olink) ? link : olink);
            }
            if (type == TagType.Breaking) currentItem.Text += "\r\n";
            if (type == TagType.Image)
            {
                if (!currentItem.isEmpty())
                {
                    returnList.Add(currentItem); 
                    currentItem = new ClientContentItem();
                }
                currentItem.Text = src;//TODO：解析onclick和src
                currentItem.TextType = ClientTextType.Photo;
                if (!string.IsNullOrEmpty(link)) currentItem.Link = link;
                else currentItem.Link = olink??"";
                returnList.Add(currentItem);
                currentItem = new ClientContentItem();
                if (!string.IsNullOrEmpty(olink))
                    currentItem.Link = olink;
                return;
            }
            if ((currentItem.Link??"")!= (olink??""))
            {
                if (!currentItem.isEmpty())
                {
                    returnList.Add(currentItem);
                    currentItem = new ClientContentItem();
                }
                currentItem.Link = olink??"";
                currentItem.TextType = string.IsNullOrEmpty(olink)?ClientTextType.Text : ClientTextType.Href;
                currentItem.Text += content;
                return;
            }
        }

        private string FormatIndexString(string prefix)
        {
            StringBuilder stringBuilder = new StringBuilder(string.Format("{4}type: {0}, properties: {1}, content: {2}, link: {3}", type, properties, content, link,prefix));
            if (ChildTags != null && ChildTags.Count > 0)
            {
                ChildTags.ForEach(x => stringBuilder.Append("\r\n" + x.FormatIndexString(prefix+"\t")));
            }
            return stringBuilder.ToString();
        }

        protected bool Equals(HtmlNode other)
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
            return Equals((HtmlNode) obj);
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
    
    public class ClientContentItem
    {
        public ClientContentItem()
        {
            Text = string.Empty;
            Link = string.Empty;
            TextType = ClientTextType.Text;
        }

        public ClientTextType TextType { get; set; }
        public string Text { get; set; }
        public string Link { get; set; }

        public bool isEmpty()
        {
            return string.IsNullOrEmpty(Text);
        }
    }
    public enum ClientTextType
            {
                /// <summary>
                /// 文本类型
                /// </summary>
                Text = 1,
                /// <summary>
                /// 超链
                /// </summary>
                Href = 2,
                /// <summary>
                /// 表情
                /// </summary>
                Expression = 3,
                /// <summary>
                /// 图片
                /// </summary>
                Photo = 4,
                /// <summary>
                /// 订单
                /// </summary>
                Order = 5
            }
}
