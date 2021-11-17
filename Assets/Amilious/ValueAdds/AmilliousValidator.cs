using System.IO;

namespace Amilious.ValueAdds
{
	public static class AmilliousValidator 
	{
	
		/// <summary>
		/// This function is for warning users who doesn't have the required Package
		/// </summary>
		/// <param name="packageName">The name of the package you want to validate</param>
		/// <returns></returns>
		public static bool ValidatePackage(string packageName)
		{
			//get all text from manifest file.
			string pack = File.ReadAllText("Packages/manifest.json");
		
			// check if package name exists
			return pack.Contains(packageName);
		}
	}
}
