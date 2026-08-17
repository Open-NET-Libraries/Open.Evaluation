namespace Open.Evaluation.Tests.Core;

[TestClass]
public class SymbolTests
{
	[TestMethod]
	public void Character_ReturnsConstructedCharacter()
	{
		var symbol = new Symbol('+', pad: true);
		symbol.Character.Should().Be('+');
	}

	[TestMethod]
	public void Constructor_PadFalse_TextIsBareCharacter()
	{
		var symbol = new Symbol('^', pad: false);
		symbol.Text.Should().Be("^");
	}

	[TestMethod]
	public void Constructor_PadTrue_TextIsPaddedWithSpaces()
	{
		var symbol = new Symbol('+', pad: true);
		symbol.Text.Should().Be(" + ");
	}

	[TestMethod]
	public void ImplicitConversion_ToChar_ReturnsCharacter()
	{
		var symbol = new Symbol('*', pad: true);
		char c = symbol;
		c.Should().Be('*');
	}

	[TestMethod]
	public void ImplicitConversion_ToString_ReturnsText()
	{
		var symbol = new Symbol('*', pad: true);
		string s = symbol;
		s.Should().Be(" * ");
	}

	[TestMethod]
	public void Constructor_WithExplicitText_UsesTextVerbatim()
	{
		var symbol = new Symbol('#', "custom");
		symbol.Character.Should().Be('#');
		symbol.Text.Should().Be("custom");
	}

	[TestMethod]
	public void Constructor_WithNullText_Throws()
	{
		Action act = () => _ = new Symbol('#', (string)null!);
		act.Should().Throw<ArgumentNullException>();
	}
}
