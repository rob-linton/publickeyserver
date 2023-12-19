using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class CertificateFingerprint
{
    private const int GridWidth = 17;
    private const int GridHeight = 9;
    private static readonly int[,] Field = new int[GridHeight, GridWidth];

	public static void DisplayCertificateFingerprintFromString(string fingerprint)
	{
		// Process the fingerprint using Drunken Bishop algorithm
                ProcessFingerprint(fingerprint);

                // Display the ASCII art representation
                DisplayAsciiArtWithBox();
	}
    public static void DisplayCertificateFingerprint(string certificatePath)
    {
        try
        {
            // Load the certificate
            X509Certificate2 cert = new X509Certificate2(certificatePath);

            // Compute the SHA-256 hash of the certificate's raw data
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(cert.RawData);
                string fingerprint = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();

                // Process the fingerprint using Drunken Bishop algorithm
                ProcessFingerprint(fingerprint);

                // Display the ASCII art representation with a box around it
                DisplayAsciiArtWithBox();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying certificate fingerprint: {ex.Message}");
        }
    }

    private static void ProcessFingerprint(string fingerprint)
    {
        int x = GridWidth / 2;
        int y = GridHeight / 2;

        foreach (char c in fingerprint)
        {
            int byteValue = Convert.ToInt32(c.ToString(), 16);
            for (int bit = 0; bit < 4; bit++)
            {
                int move = (byteValue >> bit) & 0x03;
                switch (move)
                {
                    case 0: y = Math.Max(0, y - 1); break; // up
                    case 1: x = Math.Min(GridWidth - 1, x + 1); break; // right
                    case 2: y = Math.Min(GridHeight - 1, y + 1); break; // down
                    case 3: x = Math.Max(0, x - 1); break; // left
                }
                Field[y, x]++;
            }
        }
    }

    private static void DisplayAsciiArtWithBox()
    {
        string[] symbols = { " ", ".", "o", "+", "=", "*", "B", "O", "X", "@", "%", "&", "#", "/", "^", "S", "E" };
        string horizontalBorder = "+" + new string('-', GridWidth) + "+";

        Console.WriteLine(horizontalBorder);
        for (int i = 0; i < GridHeight; i++)
        {
            Console.Write("|");
            for (int j = 0; j < GridWidth; j++)
            {
                int value = Field[i, j];
                Console.Write(symbols[Math.Min(value, symbols.Length - 1)]);
            }
            Console.WriteLine("|");
        }
        Console.WriteLine(horizontalBorder);
    }
}
