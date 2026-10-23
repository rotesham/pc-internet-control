using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace PCInternetKontrolu {
 public static class RuleIdentity {
  public const string Prefix="PCNetBlock-v1-";
  public const string Group="PC Internet Kontrolu";
  public static string Canonical(string path){if(string.IsNullOrWhiteSpace(path))throw new ArgumentException(UiText.Get("Dosya yolu boş olamaz."));return Path.GetFullPath(path);}
  public static string Name(string path,int direction){
   if(direction!=1 && direction!=2)throw new ArgumentException(UiText.Get("Geçersiz yön."));
   string canonical=Canonical(path).ToUpperInvariant();
   using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-","");return Prefix+hash+(direction==1?"-IN":"-OUT");}
  }
 }
}
