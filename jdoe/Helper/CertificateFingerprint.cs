using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class CertificateFingerprint
{
    private const int GridWidth = 16;
    private const int GridHeight = 8;
    private static readonly int[,] Field = new int[GridHeight, GridWidth];

	public static void DisplayCertificateFingerprintFromString(byte[] fingerprint)
	{
		// Process the fingerprint using Drunken Bishop algorithm
		ProcessFingerprint(fingerprint);

		// Display the ASCII art representation
		DisplayAsciiArtWithBox();
	}

	private static void ProcessFingerprint(byte[] fingerprint)
    {
        int x = GridWidth / 2;
        int y = (GridHeight / 2) + 4;

        foreach (int byteValue in fingerprint)
        {
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
