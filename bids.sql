/****** Object:  Table [dbo].[Bids]    Script Date: 7/12/2018 3:25:54 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Bids](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Bidder] [int] NOT NULL,
	[Auction] [int] NOT NULL,
	[BidOn] [datetime] NOT NULL,
	[Currency] [varchar](3) NOT NULL,
	[Amount] [float] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Bids]  WITH CHECK ADD FOREIGN KEY([Auction])
REFERENCES [dbo].[Auction] ([Id])
GO

ALTER TABLE [dbo].[Bids]  WITH CHECK ADD FOREIGN KEY([Bidder])
REFERENCES [dbo].[User] ([Id])
GO

