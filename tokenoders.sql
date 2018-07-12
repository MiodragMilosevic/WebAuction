/****** Object:  Table [dbo].[TokenOders]    Script Date: 7/12/2018 3:26:31 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TokenOders](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Buyer] [int] NOT NULL,
	[TokensAmount] [int] NOT NULL,
	[Price] [decimal](18, 0) NOT NULL,
	[Currency] [varchar](3) NOT NULL,
	[Status] [varchar](10) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TokenOders]  WITH CHECK ADD FOREIGN KEY([Buyer])
REFERENCES [dbo].[User] ([Id])
GO

