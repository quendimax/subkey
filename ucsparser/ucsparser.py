import sys
from xml.sax.handler import ContentHandler
from xml.sax import parse


class Handler(ContentHandler):

    def __init__(self, filename):
        ContentHandler.__init__(self)
        self._group = ""
        self._cp = ''
        self._fd = open(filename, 'wt', encoding='utf8')

    def startDocument(self):
        self._fd.write('<?xml version="1.0" encoding="utf-8" ?>\n<keyboard>\n')

    def endDocument(self):
        self._fd.write('</keyboard>\n')
        self._fd.close()

    def startElement(self, name, attrs):
        if name == "group":
            if "blk" in attrs:
                self._group = attrs.getValue("blk")
            else:
                self._group = ""
            if self.check_group():
                self._fd.write('\t<scheme name="%s">\n' % self._group)
        elif name == "char":
            if self.check_group():
                char_name = ""
                if "na" in attrs:
                    char_name = attrs.getValue("na")
                elif "na1" in attrs:
                    char_name = attrs.getValue("na1")
                self._cp = ''
                if "cp" in attrs:
                    self._cp = attrs.getValue("cp")
                if "Lower" in attrs and attrs["Lower"] == "Y":
                    self._cp = ''

                if self._cp:
                    self._fd.write('\t\t<key>\n')
                    unicode_number = int(self._cp, 16)
                    try:
                        sym = chr(unicode_number)
                    except ValueError as e:
                        print(str(e), file=sys.stderr)
                        sym = '&#x%s' % self._cp
                    self._fd.write('\t\t\t<text>%s</text>\n' % sym)
                    if char_name.startswith('COMBINING'):
                        self._fd.write('\t\t\t<title>◌%s</title>\n' % sym)
                    self._fd.write('\t\t\t<tooltip>%s</tooltip>\n' % char_name)

    def endElement(self, name):
        if name == "group" and self.check_group():
            self._fd.write('\t</scheme>\n')
        elif name == "char" and self.check_group() and self._cp:
            self._fd.write("\t\t</key>\n")

    def check_group(self):
        if self._group and self._group in {"Latin_1_Sup", "Latin_Ext_A", "Latin_Ext_B", "Latin_Ext_D", "Latin_Ext_E",
                                           "Latin_Ext_Additional", "Modifier_Letters", "IPA_Ext", "Phonetic_Ext",
                                           "Phonetic_Ext_Sup", "Diacriticals", "Diacriticals_Ext", "Diacriticals_Sup",
                                           "Cyrillic", "Cyrillic_Ext_A", "Cyrillic_Ext_B", "Cyrillic_Ext_C",
                                           "Cyrillic_Sup", "Geometric_Shapes", "Glagolitic", "Latin_Ext_C",
                                           "Glagolitic_Sup", "Runic", "Emoticons"}:
            return True
        return False


if __name__ == '__main__':
    handler = Handler('test.xml')
    parse("ucd.all.grouped.xml", handler)
