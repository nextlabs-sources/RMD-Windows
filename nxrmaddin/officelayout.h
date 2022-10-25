#pragma once

#define WORD_LAYOUT_XML_16 \
L"<?xml version=\"1.0\" encoding=\"utf-8\"?> \
 <customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\" onLoad=\"OnLoad\" loadImage=\"LoadImage\"> \
 	<commands>\
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInfo\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabOfficeStart\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabRecent\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileClose\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FilePrintQuick\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPrint\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabShare\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPublish\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ApplicationOptionsDialog\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"AdvancedFileProperties\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"UpgradeDocument\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSendAsAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsPdfEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsXpsEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileInternetFax\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabHome\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabWordDesign\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPageLayoutWord\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabReferences\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabMailings\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabReviewWord\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenshotInsertGallery\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenClipping\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"OleObjectInsertMenu\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"OleObjectctInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Paste\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGallery\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGalleryMini\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteSpecialDialog\"/> \
	    <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShowClipboard\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteSetDefault\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Cut\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Copy\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ObjectSaveAsPicture\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"DigitalPrint\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShareDocument\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Collaborate\"/> \
	</commands> \
 </customUI>"



#define EXCEL_LAYOUT_XML_16 \
L"<?xml version=\"1.0\" encoding=\"utf-8\"?> \
 <customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\" onLoad=\"OnLoad\" loadImage=\"LoadImage\"> \
 	<commands>\
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInfo\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabOfficeStart\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabRecent\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileClose\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FilePrintQuick\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPrint\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabShare\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPublish\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Publish2Tab\" /> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ApplicationOptionsDialog\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"AdvancedFileProperties\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"UpgradeDocument\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSendAsAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsPdfEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsXpsEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileInternetFax\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabHome\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabFormulas\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabReview\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabData\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"SheetMoveOrCopy\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenshotInsertGallery\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenClipping\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"OleObjectctInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Paste\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGallery\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGalleryMini\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteSpecialDialog\"/> \
	    <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShowClipboard\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Cut\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Copy\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"CopyAsPicture\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShareDocument\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Collaborate\"/> \
	</commands> \
 </customUI>"


#define POWERPNT_LAYOUT_XML_16 \
L"<?xml version=\"1.0\" encoding=\"utf-8\"?> \
 <customUI xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\" onLoad=\"OnLoad\" loadImage=\"LoadImage\"> \
 	<commands>\
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInfo\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabOfficeStart\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabRecent\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileClose\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FilePrintQuick\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabSave\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPrint\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabShare\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabPublish\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ApplicationOptionsDialog\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"AdvancedFileProperties\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"UpgradeDocument\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSendAsAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsPdfEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileEmailAsXpsEmailAttachment\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileInternetFax\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabHome\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabDesign\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabTransitions\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabAnimations\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabSlideShow\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabReview\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabDeveloper\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"TabView\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenshotInsertGallery\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ScreenClipping\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"OleObjectInsertMenu\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"OleObjectctInsert\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Paste\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGallery\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteGalleryMini\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"PasteSpecialDialog\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShowClipboard\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Cut\"/> \
		<command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Copy\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ObjectSaveAsPicture\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"FileSaveAs\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"ShareDocument\"/> \
        <command getEnabled=\"CheckMsoButtonStatus\" idMso=\"Collaborate\"/> \
	</commands> \
 </customUI>"

