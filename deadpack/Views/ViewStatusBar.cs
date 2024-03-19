
using Terminal.Gui;

class ViewStatusBar 
{
	// public property for the menu
	public StatusBar StatusBar { get; set; }

	public ViewStatusBar()
	{
		// create a status bar
		StatusBar = new StatusBar(new StatusItem[] {
			new StatusItem(Key.F9, "~F9~ Menu", () => { }),
			new StatusItem(Key.CtrlMask | Key.R, "~Ctrl-R~ Refresh", () => 
			{ 
				ViewMenu.MenuSend(true);
			}),
		});
	}
}
