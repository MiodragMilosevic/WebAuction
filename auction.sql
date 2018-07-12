/****** Object:  Table [dbo].[Auction]    Script Date: 7/12/2018 3:24:45 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Auction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](255) NOT NULL,
	[AuctionTime] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[OpenedOn] [datetime] NULL,
	[CompletedOn] [datetime] NULL,
	[StartPrice] [decimal](18, 0) NOT NULL,
	[CurrentPrice] [decimal](18, 0) NULL,
	[Currency] [varchar](3) NOT NULL,
	[State] [varchar](20) NULL,
	[IMG] [varchar](255) NOT NULL,
	[User] [int] NULL,
	[FullName] [varchar](255) NULL,
	[WHo] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

