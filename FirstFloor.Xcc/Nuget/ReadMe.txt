XAML CONDITIONAL COMPILATION
============================
Welcome to XCC, a preprocessor adding conditional compilation support to XAML files.

Supports conditional compilation of both XML elements and attributes by making use of custom XML namespaces. XCC hooks into the MSBuild process and transforms XAML files just before they are handed to the XAML compiler.

!! IMPORTANT
This is a proof-of-concept.
No guarantees whatsoever.
Use at your own risk.
!!

USAGE
-----
1) Add conditional compilation symbols to the XAML root;

xmlns:win81="condition:WINDOWS_APP"
xmlns:wp81="condition:WINDOWS_PHONE_APP"

2) Add or update the mc:Ignorable

mc:Ignorable="d win81 wp81"

3) Add or update mc:ProcessContent to enable Intellisense inside conditional regions

mc:ProcessContent="win81:* wp81:*"

Consider the following XAML snippet

<StackPanel>
	<Button Content="Default button"  />
	<Button Content="Colored button" win81:Background="Red" wp81:Background="Green" />
	<win81:Button Content="Windows 8.1" />
	<wp81:Button Content="Windows Phone 8.1" />
        
	<win81:Grid>
		<Button Content="Another Windows 8.1 button" />
	</win81:Grid>
</StackPanel>

With WINDOWS_APP symbol this results in the following XAML:

<StackPanel>
	<Button Content="Default button"  />
	<Button Content="Colored button" Background="Red" />
	<Button Content="Windows 8.1" />
        
	<Grid>
		<Button Content="Another Windows 8.1 button" />
	</Grid>
</StackPanel>

With WINDOWS_PHONE_APP symbol this results in the following XAML:

<StackPanel>
	<Button Content="Default button"  />
	<Button Content="Colored button" Background="Green" />

	<Button Content="Windows Phone 8.1" />
        



</StackPanel>