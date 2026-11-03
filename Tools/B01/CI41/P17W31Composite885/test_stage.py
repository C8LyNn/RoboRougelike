#!/usr/bin/env python3
import unittest
from unittest.mock import patch
from stage import (SHA, PINS, git_blob, original, validate_path, adapt_qa,
                   assert_new_writer, assert_admission_guard, CORE, SUBSTITUTIONS)

class CompositeV3Guards(unittest.TestCase):
    def test_exact_immutable_inputs(self):
        self.assertEqual(len(PINS), 17)
        self.assertTrue(all(len(s)==40 and all(c in "0123456789abcdef" for c in s) for s in SHA.values()))
    def test_one_selected_writer_excludes_old_v2_from_compilation(self):
        self.assertEqual([x for x in CORE if x.startswith("Save")], ["SaveV3.cs"])
        self.assertEqual([x for x in CORE if x.startswith("Session")], ["SessionV3.cs"])
    def test_identical_git_blob_measured(self):
        self.assertEqual(git_blob(b"hello\n"),"ce013625030ba8dba906f756967f9e9ca394464a")
    def test_bit_mutant_denied(self):
        with patch("stage.git",return_value=b"tampered"):
            with self.assertRaisesRegex(AssertionError,"OID mismatch"):
                original("SaveV3.cs")
    def test_reject_unsafe_paths(self):
        for p in ["../escape.cs","/tmp/file.cs","A\\B.cs","bad\0.cs","a/../../b"]:
            with self.subTest(path=p),self.assertRaises(ValueError):
                validate_path(p)
    def test_adapt_test_only_v2_public_abi(self):
        sample="; ".join(a for a,b in SUBSTITUTIONS)
        result=adapt_qa(sample)
        for a,b in SUBSTITUTIONS:
            self.assertIn(b,result)
            self.assertNotIn(a,result)
    def test_adapt_original_qa_missing_source_symbol_rejected(self):
        with self.assertRaisesRegex(AssertionError,"schema drift"):
            adapt_qa("no API")
    def test_replace_two_args_compatibility_contract(self):
        src="File.Replace(staging,authoritative,null);File.Move(staging,authoritative);B0162G02 save.envelope"
        self.assertTrue(assert_new_writer(src))
    def test_forbid_legacy_hashdata_or_tohex_or_move3(self):
        base="File.Replace(staging,authoritative,null);File.Move(staging,authoritative);B0162G02 save.envelope "
        for poison in ["SHA256.HashData(input)","Convert.ToHexString(d)","File.Move(a,b,true)"]:
            with self.subTest(poison=poison),self.assertRaises(AssertionError):
                assert_new_writer(base+poison)
    def test_forbid_destructive_move_or_missing_envelope(self):
        src="File.Replace(staging,authoritative,null);File.Move(staging,authoritative);B0162G02 save.envelope"
        for tamper in [src+"File.Delete(authoritative)",src.replace("B0162G02","B0162G99"),src.replace("File.Replace(staging,authoritative,null)","")]:
            with self.subTest(tamper=tamper[:50]),self.assertRaises(AssertionError):
                assert_new_writer(tamper)

    def test_11_real_v7_admission_gate_required_on_disk_and_gateway(self):
        source=("EconomyV7AdmissionW29.TryAdmit;"
                "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner;"
                "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner;")
        session="EconomyV7AdmissionW29.TryAdmit"
        self.assertTrue(assert_admission_guard(source, session))
        for missing in ("EconomyV7AdmissionW29.TryAdmit",
                        "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner",
                        "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner"):
            with self.subTest(token=missing), self.assertRaises(AssertionError):
                assert_admission_guard(source.replace(missing,"removed"),session)
        with self.assertRaises(AssertionError):
            assert_admission_guard(source,"no original gateway admission")

if __name__=="__main__":unittest.main()
