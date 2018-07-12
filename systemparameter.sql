/****** Object:  Table [dbo].[SystemParameter]    Script Date: 7/12/2018 3:26:13 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SystemParameter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RecentAuction] [int] NOT NULL,
	[DefaultAuctionTime] [int] NOT NULL,
	[SilverPackage] [int] NOT NULL,
	[GoldPackage] [int] NOT NULL,
	[PlatinumPackage] [int] NOT NULL,
	[Currency] [varchar](3) NOT NULL,
	[TokensPrice] [decimal](18, 0) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

