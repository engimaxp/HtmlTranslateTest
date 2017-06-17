using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HtmlTranslateTest
{
    [TestFixture]
    public class TestClass
    {
        private static void PageBulkOperateDB<T>(Action<IList<T>> function, IList<T> list)
        {
            int PageSize = 4;//BulkInsert的最大插入记录是1万条数据
            int CurPage = 1;
            while ((CurPage - 1) * PageSize < list.Count)
            {
                function(list.Take(PageSize * CurPage).Skip(PageSize * (CurPage - 1)).ToList());
                CurPage += 1;
            }
        }
        [Test]
        public void Test()
        {
            var lista = new List<int>();
            var list = new List<int>(new int[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14});
            PageBulkOperateDB(x => { lista.AddRange(x); }, list);
            Assert.AreEqual(lista,list);
        }
        
        [Test]
        public void Test2()
        {
            var lista = new List<int>(new int[]{0,1,2});
            var list = new List<int>(new int[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14});
            list = list.Select(r => r%3).Distinct().ToList();
            Assert.AreEqual(lista,list);
        }
        [Test]
        public void Test3()
        {
            Assert.IsTrue(",e21535731,m175673171,13402014201,2038911635,wwwwww,13916983966,13564950012,63757559,2000739530,67036229,ruodian,1101616736,m272687463,e00234773,m172683879,13761226082,m502443462,m95524188,m353786436,m38180546,".Contains("," + "M38180546".ToLower() + ","));
        }

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
        public void TestTranscodeToJsonObj2()
        {

            string mock = "亲亲>您的<p>身</p>份证是<br/>丢失<a>wwww</a>了还是没有带呢>";
            Regex rgx = new Regex(@"<img(.+?[^/]+)>");
            mock = rgx.Replace(mock, "<img$1/>");

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

        private string encodeNoneHtmlChar(string html)
        {
            Regex TagPattern = new Regex(@"(?mi)(?:<\s*(?:[a-z]+)\s*)\S*?(?:(?:<\s*/[a-z]*)?[/]?\s*>)|(?:<\s*/[a-z]*?[/]?\s*>)");//匹配html标签头尾
            MatchCollection mc = TagPattern.Matches(html);
            int curPosition = 0;
            StringBuilder returnString = new StringBuilder();
            
            foreach (Match match in mc)
            {
                returnString.Append(HttpUtility.HtmlEncode(html.Substring(curPosition, match.Index - curPosition)));
                returnString.Append(match.Value);
                curPosition += match.Index - curPosition + match.Length;
            }
            if (curPosition < html.Length - 1)
            {
                returnString.Append(HttpUtility.HtmlEncode(html.Substring(curPosition, html.Length - curPosition)));
            }
            return returnString.ToString();
        }

        private string TransCodeHtmlToList(string html)
        {
            //htmlencode
            html = encodeNoneHtmlChar(html);
            //htmlToTree
            var mockHtmlTag = DecodeHtmlTag(html);
            //TreeToList
            List<ClientContentItem> returnList = mockHtmlTag.BackOrderTravel();
            
            //List中的特殊处理
            if (returnList != null && returnList.Count > 0)
            {
                //Html解码
                returnList.ForEach(r =>
                {
                    if(!string.IsNullOrEmpty(r.Text))
                        r.Text = HttpUtility.HtmlDecode(r.Text);
                    if (!string.IsNullOrEmpty(r.Link))
                        r.Link = HttpUtility.HtmlDecode(r.Link);
                });
                //去掉最后多余的换行符
                while (returnList.Count > 0 && !string.IsNullOrEmpty(returnList.Last().Text) && returnList.Last().Text.EndsWith("\r\n"))
                {
                    returnList.Last().Text = returnList.Last().Text.Substring(0, returnList.Last().Text.LastIndexOf("\r\n", StringComparison.Ordinal));
                    if (string.IsNullOrEmpty(returnList.Last().Text) && returnList.Last().TextType == ClientTextType.Text)
                    {
                        returnList.RemoveAt(returnList.Count - 1);
                    }
                }
                for (int i = 0; i < returnList.Count-1; i++)
                {
                    if (returnList[i].TextType == ClientTextType.Photo &&
                        returnList[i + 1].TextType == ClientTextType.Href &&
                        !string.IsNullOrEmpty(returnList[i + 1].Link) &&
                        string.IsNullOrEmpty(returnList[i + 1].Text))
                    {
                        returnList[i].Link = returnList[i + 1].Link;
                    }
                }
                //过滤邮件和电话链接，产生新的Type
                foreach (var clientOntentItem in returnList.Where(r => r.TextType == ClientTextType.Href && !string.IsNullOrEmpty(r.Link)))
                {
                    if (clientOntentItem.Link.ToLower().Contains("mailto:"))
                    {
                        clientOntentItem.Link = string.Empty;
                        clientOntentItem.TextType = ClientTextType.Email;
                    }
                    if (clientOntentItem.Link.ToLower().Contains("telto:"))
                    {
                        clientOntentItem.Link = string.Empty;
                        clientOntentItem.TextType = ClientTextType.Tel;
                    }
                }
            }
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
        private static readonly Regex regexFun = new Regex(@"(?i)\bchatUrlJumpLib.Jump\b\('(?<url>.*)\',(?<type>.*)\)");
        private static readonly Regex REGEX_URL = new Regex(@"(?i)((http|https)://)?(www.)?(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,9})*(/[a-zA-Z0-9\&#,%_\./-~-]*)?");
        private static readonly Regex REGEX_HREF = new Regex(@"(?i)href=""\s*(.+?)\s*""");
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
        private string _link;
        public string link
        {
            get
            {
                if (!string.IsNullOrEmpty(_link)) return _link;
                if (string.IsNullOrEmpty(properties)) return string.Empty;
                MatchCollection mcFun = regexFun.Matches(properties);
                if (mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups["url"].Value;
                    return _link;
                }
                mcFun = REGEX_HREF.Matches(properties);
                if(mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups[1].Captures[0].Value;
                    return _link;
                }
                mcFun = REGEX_URL.Matches(properties);
                if (mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups[0].Captures[0].Value;
                    return _link;
                }
                return string.Empty;
            }
            set { _link = value; }
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
            if (child.type == TagType.Default && !string.IsNullOrEmpty(child.content))
                child.content = child.content.Replace("&nbsp;", " ");
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
            if (type != TagType.Image && (((currentItem.Link ?? "") == (link ?? "")) && (currentItem.Link ?? "") == (olink ?? "")))
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
            if ((currentItem.Link ?? "") != (link ?? ""))
            {
                if (!currentItem.isEmpty())
                {
                    returnList.Add(currentItem);
                    currentItem = new ClientContentItem();
                }
                currentItem.Link = link ?? "";
                currentItem.TextType = string.IsNullOrEmpty(link) ? ClientTextType.Text : ClientTextType.Href;
                currentItem.Text += content;
                return;
            }
            if ((currentItem.Link ?? "") != (olink ?? ""))
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

        public List<ClientContentItem> BackOrderTravel()
        {
            List<ClientContentItem> reList = new List<ClientContentItem>();
            //后续遍历
            if (ChildTags != null && ChildTags.Count > 0)
                foreach (var r in ChildTags) 
                    AddClientContentItems(ref reList, r.BackOrderTravel(),this.link);
            
            //访问根节点
            ClientContentItem item = TransCurrentNodeToClientContentItem();

            //加入根节点
            if (item != null)
            {
                if (reList.Count > 0)
                {
                    var lastitem = reList.Last();
                    if (lastitem.JoinAbleWith(item))
                    {
                        lastitem.JoinWith(item);
                    }
                    else
                    {
                        reList.Add(item);
                    }
                }
                else
                {
                    reList.Add(item);
                }
                
            }

            return reList;
        }

        private ClientContentItem TransCurrentNodeToClientContentItem()
        {
            if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(link) && this.type == TagType.Default)
                return null;
            var currentItem = new ClientContentItem();
            if (this.type == TagType.Image)
            {
                currentItem.Text = src;//TODO：解析onclick和src
                currentItem.TextType = ClientTextType.Photo;
                if (!string.IsNullOrEmpty(link)) currentItem.Link = link;
            }
            else if (this.type == TagType.Breaking)
            {
                if (!string.IsNullOrEmpty(content))
                    currentItem.Text = content;
                else
                    currentItem.Text = string.Empty;
                currentItem.Text += "\r\n";
                currentItem.TextType = ClientTextType.Text;
                if (!string.IsNullOrEmpty(link))
                {
                    currentItem.Link = link;
                    currentItem.TextType = ClientTextType.Href;
                }
            }
            else if (this.type == TagType.Default && !string.IsNullOrEmpty(link))
            {
                if (!string.IsNullOrEmpty(content))
                    currentItem.Text = content;
                currentItem.Link = link;
                currentItem.TextType = ClientTextType.Href;
            }
            else
            {
                if (!string.IsNullOrEmpty(content))
                    currentItem.Text = content;
                currentItem.TextType = ClientTextType.Text;
            }
            return currentItem;
        }

        /// <summary>
        /// 合并List
        /// </summary>
        /// <param name="reList"></param>
        /// <param name="backOrderTravel"></param>
        /// <param name="olink">上层链接</param>
        private void AddClientContentItems(ref List<ClientContentItem> reList, List<ClientContentItem> backOrderTravel,string olink)
        {
            if (reList == null || reList.Count == 0) {
                reList = backOrderTravel;
                return;
            }
            if (backOrderTravel == null || backOrderTravel.Count == 0) return;
            //上层Link影响下层内容
            if (!string.IsNullOrEmpty(olink))
            {
                backOrderTravel.Where(r=>r.TextType == ClientTextType.Text).ToList().ForEach(x=>
                {
                    x.TextType = ClientTextType.Href;
                    x.Link = olink;
                });
                backOrderTravel.Where(r=>r.TextType == ClientTextType.Photo).ToList().ForEach(x=>x.Link = olink);
            }
            var lastitem = reList.Last();
            foreach (ClientContentItem curItem in backOrderTravel)
            {
                if (lastitem.JoinAbleWith(curItem))
                {
                    lastitem.JoinWith(curItem);
                }
                else
                {
                    lastitem = curItem;
                    reList.Add(lastitem);
                }
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

        public bool JoinAbleWith(ClientContentItem curItem)
        {
            if (this.TextType == curItem.TextType)
            {
                switch (this.TextType)
                {
                    case ClientTextType.Href:
                    {
                        return this.Link == curItem.Link;
                    }
                    case ClientTextType.Text:
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void JoinWith(ClientContentItem curItem)
        {
            if (this.TextType == curItem.TextType) ;
            {
                switch (this.TextType)
                {
                    case ClientTextType.Href:
                    {
                        if (this.Link == curItem.Link)
                        {
                            this.Text = (this.Text ?? String.Empty) + curItem.Text;
                        }
                        break;
                    }
                    case ClientTextType.Text:
                    {
                        this.Text = (this.Text ?? String.Empty) + curItem.Text;
                        break;
                    }
                }
            }
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
                /// 电话
                /// </summary>
                Tel = 5,

                /// <summary>
                /// 邮件
                /// </summary>
                Email = 6
            }
}
