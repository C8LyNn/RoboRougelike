import unittest
from w9_preflight import directives,asmdef
class TestPreflight(unittest.TestCase):
    def test_missing_endif(self):self.assertEqual(directives("#if UNITY_EDITOR\nx\n"),["MISSING_ENDIF_1"])
    def test_balanced(self):self.assertEqual(directives("#if UNITY_EDITOR\nx\n#endif\n"),[])
    def test_nested(self):self.assertEqual(directives("#if A\n#if B\n#endif\n#endif"),[])
    def test_extra_close(self):self.assertIn("STRAY_ENDIF_1",directives("#endif"))
    def test_double_else(self):self.assertIn("DUPLICATE_ELSE_3",directives("#if A\n#else\n#else\n#endif"))
    def test_elif_after_else(self):self.assertIn("ELIF_AFTER_ELSE_3",directives("#if A\n#else\n#elif B\n#endif"))
    def test_inline_literal(self):self.assertEqual(directives('var x = "#if HELLO";'),[])
    def test_elif_legal(self):self.assertEqual(directives("#if A\n#elif B\n#else\n#endif"),[])
    def test_dup_runner(self):self.assertIn("DOUBLE_TEST_RUNNER",asmdef({"references":["A","UnityEngine.TestRunner"],"optionalUnityReferences":["TestAssemblies"]}))
    def test_valid_runner(self):self.assertEqual(asmdef({"references":["A"],"optionalUnityReferences":["TestAssemblies"]}),[])
    def test_repeated_ref(self):self.assertIn("DUPLICATE_REFERENCE",asmdef({"references":["A","A"]}))
    def test_malformed_ref(self):self.assertIn("INVALID_ASMDEF",asmdef({"references":None}))
if __name__=="__main__":unittest.main()
