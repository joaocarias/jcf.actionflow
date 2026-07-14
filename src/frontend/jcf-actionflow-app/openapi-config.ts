import type { ConfigFile } from '@rtk-query/codegen-openapi'

const config: ConfigFile = {
  schemaFile: 'http://localhost:5000/openapi/v1.json',
  apiFile: './src/lib/api/baseApi.ts',
  apiImport: 'baseApi',
  outputFile: './src/lib/api/generated.ts',
  exportName: 'generatedApi',
  hooks: true,
}

export default config
