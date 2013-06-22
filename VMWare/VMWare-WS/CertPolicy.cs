using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections;

namespace P2PBackup.Virtualization.Handlers{
   /// <summary>
   /// SSL Certificate policy management.
   /// </summary>
   public class CertPolicy : ICertificatePolicy {
      private enum CertificateProblem  : uint {
         CertEXPIRED                   = 0x800B0101,
         CertVALIDITYPERIODNESTING     = 0x800B0102,
         CertROLE                      = 0x800B0103,
         CertPATHLENCONST              = 0x800B0104,
         CertCRITICAL                  = 0x800B0105,
         CertPURPOSE                   = 0x800B0106,
         CertISSUERCHAINING            = 0x800B0107,
         CertMALFORMED                 = 0x800B0108,
         CertUNTRUSTEDROOT             = 0x800B0109,
         CertCHAINING                  = 0x800B010A,
         CertREVOKED                   = 0x800B010C,
         CertUNTRUSTEDTESTROOT         = 0x800B010D,
         CertREVOCATION_FAILURE        = 0x800B010E,
         CertCN_NO_MATCH               = 0x800B010F,
         CertWRONG_USAGE               = 0x800B0110,
         CertUNTRUSTEDCA               = 0x800B0112
      }

      private static Hashtable problem2text_;
      private Hashtable request2problems_; // WebRequest -> ArrayList of error codes

      public CertPolicy() {
         if (problem2text_ == null) {
            problem2text_ = new Hashtable();

            problem2text_.Add((uint) CertificateProblem.CertEXPIRED, 
               "A required certificate is not within its validity period.");
            problem2text_.Add((uint) CertificateProblem.CertVALIDITYPERIODNESTING,
               "The validity periods of the certification chain do not nest correctly.");
            problem2text_.Add((uint) CertificateProblem.CertROLE,
               "A certificate that can only be used as an end-entity is being used as a CA or visa versa.");
            problem2text_.Add((uint) CertificateProblem.CertPATHLENCONST,
               "A path length constraint in the certification chain has been violated.");
            problem2text_.Add((uint) CertificateProblem.CertCRITICAL,
               "An extension of unknown type that is labeled 'critical' is present in a certificate.");
            problem2text_.Add((uint) CertificateProblem.CertPURPOSE,
               "A certificate is being used for a purpose other than that for which it is permitted.");
            problem2text_.Add((uint) CertificateProblem.CertISSUERCHAINING,
               "A parent of a given certificate in fact did not issue that child certificate.");
            problem2text_.Add((uint) CertificateProblem.CertMALFORMED,
               "A certificate is missing or has an empty value for an important field, such as a subject or issuer name.");
            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDROOT,
               "A certification chain processed correctly, but terminated in a root certificate which isn't trusted by the trust provider.");
            problem2text_.Add((uint) CertificateProblem.CertCHAINING,
               "A chain of certs didn't chain as they should in a certain application of chaining.");
            problem2text_.Add((uint) CertificateProblem.CertREVOKED,
               "A certificate was explicitly revoked by its issuer.");
            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDTESTROOT,
               "The root certificate is a testing certificate and the policy settings disallow test certificates.");
            problem2text_.Add((uint) CertificateProblem.CertREVOCATION_FAILURE,
               "The revocation process could not continue - the certificate(s) could not be checked.");
            problem2text_.Add((uint) CertificateProblem.CertCN_NO_MATCH,
               "The certificate's CN name does not match the passed value.");
            problem2text_.Add((uint) CertificateProblem.CertWRONG_USAGE,
               "The certificate is not valid for the requested usage.");
            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDCA,
               "Untrusted CA");
         }

         request2problems_ = new Hashtable();
      }

      // ICertificatePolicy
      public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest request, int problem) {
         if (problem == 0) {
            // Check whether we have accumulated any problems so far:
            ArrayList problemArray = (ArrayList) request2problems_[request];
            if (problemArray == null) {
               // No problems so far
               return true;
            }

            string problemList = "";
            foreach (uint problemCode in problemArray) {
               string problemText = (string) problem2text_[problemCode];
               if (problemText == null) {
                  problemText = "Unknown problem";
               }
               problemList += "* " + problemText + "\n\n";
            }

            request2problems_.Remove(request);
            //System.Console.WriteLine("There were one or more problems with the server certificate:\n\n" + problemList);
            return true;


         } else {
            // Stash the problem in the problem array:
            ArrayList problemArray = (ArrayList) request2problems_[request];
            if (problemArray == null) {
               problemArray = new ArrayList();
               request2problems_[request] = problemArray;
            }
            problemArray.Add((uint) problem);
            return true;
         }
      }   
   }
}
