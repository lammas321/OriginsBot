using NetCord.Rest;

namespace OriginsBot.Extensions
{
    public static class EmbedPropertiesExtensions
    {
        private const int MaxTitleLength = 256;
        private const int MaxDescriptionLength = 2048; // Actually 4096

        private const int MaxAuthorNameLength = 256;
        private const int MaxFooterTextLength = 256; // Actually 2048

        private const int MaxFieldNameLength = 256;
        private const int MaxFieldValueLength = 1024;

        private const int MaxLength = 6000;
        private const int MaxFields = 25;
        private const int MaxEmbeds = 10;


        public static List<EmbedProperties> MakeCompliant(this EmbedProperties self)
        {
            if (self.Title?.Length > MaxTitleLength)
                self.Title = self.Title[..MaxTitleLength];

            if (self.Description?.Length > MaxDescriptionLength)
                self.Description = self.Description[..MaxDescriptionLength];

            if (self.Author?.Name?.Length > MaxAuthorNameLength)
                self.Author.Name = self.Author.Name[..MaxAuthorNameLength];

            if (self.Footer?.Text?.Length > MaxFooterTextLength)
                self.Footer.Text = self.Footer.Text[..MaxFooterTextLength];

            if (self.Fields == null)
                return [self];


            IEnumerable<EmbedFieldProperties> allFields = self.Fields;
            self.Fields = null;

            int contentBaseLength =
                self.Title?.Length ?? 0 +
                self.Author?.Name?.Length ?? 0 +
                self.Footer?.Text?.Length ?? 0;

            int contentLength = contentBaseLength + self.Description?.Length ?? 0;

            List<EmbedProperties> embeds = [];
            EmbedProperties embed = self;
            List<EmbedFieldProperties> fields = [];


            foreach (EmbedFieldProperties field in allFields)
            {
                if (field.Name?.Length > MaxFieldNameLength)
                    field.Name = field.Name[..MaxFieldNameLength];

                if (field.Value?.Length > MaxFieldValueLength)
                    field.Value = field.Value[..MaxFieldValueLength];

                int fieldLength = field.Name?.Length ?? 0 + field.Value?.Length ?? 0;
                if (contentLength + fieldLength > MaxLength || fields.Count >= MaxFields)
                {
                    contentLength = contentBaseLength;

                    embed.Fields = fields;
                    embeds.Add(embed);
                    fields = [];

                    embed = new()
                    {
                        Title = self.Title,
                        Url = self.Url,
                        Timestamp = self.Timestamp,
                        Color = self.Color,
                        Footer = self.Footer,
                        Author = self.Author,
                    };
                }

                fields.Add(field);
                contentLength += fieldLength;
            }

            embed.Fields = fields;
            embeds.Add(embed);
            return embeds[..MaxEmbeds];
        }
    }
}