import {Component,Input} from '@angular/core';
import { HttpClient, HttpResponse, HttpHeaders } from '@angular/common/http'
import {trigger, animate, style, group, animateChild, query, stagger, transition, state} from '@angular/animations';
import { Location } from '@angular/common'
import { DataService } from '../../services/data.service'
import { HubService } from '../../services/hub.service'
import { Router } from '@angular/router'
import { ImageItem } from './image.item'
import { TextItem } from './text.item'
import { AudioItem } from './audio.item'
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap'
import { AppInsightsService } from '../../services/app-insights.service'

@Component({
    selector: 'app-items',
    templateUrl: './item.component.html',
    styleUrls: ['./item.component.css'],
    animations:[
        trigger('fadeshow', [
            transition(':enter', [
                style({ opacity: 0 }),
                animate('1000ms ease-in', style({ opacity: 1 }))
            ]),
            transition(':leave', [
                style({ opacity: 1 }),
                animate('1000ms ease-in', style({ opacity: 0 }))
            ])
        ])
    ]
})

export class ItemComponent {

    injectedCategoryId: string
    injectedUserId: string
    showSubmit: boolean=false
    showAudio: boolean=false
    showTextSubmit: boolean=false
    loaded: boolean=false
    imageErrorMessage: string
    audioErrorMessage: string
    previewImage: any
    previewAudio: any
    imageAsArrayBuffer: any
    audioAsArrayBuffer: any
    imageMimeType: string
    audioMimeType : string
    image_items : Array<ImageItem>
    text_items: Array<TextItem>
    audio_items: Array<AudioItem>
    inputText: string
    closeResult: string
    modalContent: string
    currentText:  string
    currentTranscript: string
    updatedText: string
    update : boolean=false
    showHorizontalSpinner: false
    spinnerMessage: string
    audioFileName: string
    injectedCategoryName: string
    displayUserId: string

    constructor(private data:DataService,private hub: HubService,
                private http:HttpClient,private modalService: NgbModal, 
                private appInsightsService: AppInsightsService,private router: Router){
        this.imageErrorMessage = ""
        this.audioErrorMessage = ""
        this.previewImage = "#"
        this.image_items = new Array<ImageItem>()
        this.text_items = new Array<TextItem>()
        this.audio_items = new Array<AudioItem>()
        this.spinnerMessage = ""
        this.appInsightsService.logPageView('Items Page');
    }

    ngOnInit(){
        this.data.currentUser.subscribe(injectedUserId => {      
                this.injectedUserId = injectedUserId
        })
        this.data.currentCategory.subscribe(injectedCategoryId => this.injectedCategoryId = injectedCategoryId)
        this.data.currentCategoryName.subscribe(injectedCategoryName => this.injectedCategoryName = injectedCategoryName)
        this.data.images_list.subscribe(images => this.image_items = images)
        this.data.text_list.subscribe(text => this.text_items = text)
        this.data.audio_list.subscribe(audio => this.audio_items = audio)
    }

    ngAfterContentInit() {
        this.displayUserId = this.injectedUserId.split("@")[0]
        this.listenToEvents()
        this.getAllImages()
        this.getAllText()
        this.getAllAudio()
    }

    listenToEvents(){

        // Add event listeners here

        this.hub.getHubConnection().on('onImageCaptionUpdated', (imageId:string,eventData: any) => {
            let image : ImageItem = this.fetchImageObject(imageId)
            image.setCaption(eventData.caption)
            image.showNotification = true
            this.appInsightsService.logEvent("Image Caption Updated",{image : imageId})
        })
        this.hub.getHubConnection().on('onTextUpdated', (textId:string,eventData: any) => {
            let text : TextItem = this.fetchTextObject(textId)
            text.setText(eventData.preview)
            text.showNotification = true
            this.appInsightsService.logEvent("Text Note Updated",{text : textId})
        })
        this.hub.getHubConnection().on('onAudioTranscriptUpdated', (audioId:string,eventData: any) => {
            let audio : AudioItem = this.fetchAudioObject(audioId)
            audio.setTranscript(eventData.transcriptPreview)
            this.getAudio(audio,audioId)
            audio.showNotification = true
            this.appInsightsService.logEvent("Audio Transcript Updated",{audio : audioId})
        })

        this.hub.getHubConnection().on('onImageCreated', (imageId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Image Created",{image : imageId})
        })

        this.hub.getHubConnection().on('onImageDeleted', (imageId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Image Deleted",{image : imageId})
        })

        this.hub.getHubConnection().on('onAudioCreated', (audioId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Audio Created",{audio : audioId})
        })

        this.hub.getHubConnection().on('onAudioDeleted', (audioId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Audio Deleted",{audio : audioId})
        })

        this.hub.getHubConnection().on('onTextCreated', (textId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Text Created",{text : textId})
        })

        this.hub.getHubConnection().on('onTextDeleted', (textId:string,eventData: any) => {
             //** Add event handling code for category creation here */
             this.appInsightsService.logEvent("Text Deleted",{text : textId})
        })
    }

    getAllImages(){
        
           for (let image of this.image_items){

                var image_get_url = location.origin + "/api/Image/"+ image.id + "/?userId=" + this.injectedUserId;
                const imageReq = this.http.get(image_get_url)

                imageReq.subscribe((data:any) => {
                    var currentImage = JSON.parse(data)
                    let fetchedImage: ImageItem = this.fetchImageObject(currentImage.id)
                    fetchedImage.setPreviewUrl(currentImage.imageUrl)
                    fetchedImage.setCaption(currentImage.caption)
                })
            }
            this.loaded = true
        
    }

    getAllText(){

        for (let text of this.text_items){
            var text_get_url = location.origin + "/api/Text/"+ text.id + "/?userId=" + this.injectedUserId;
            const textReq = this.http.get(text_get_url)

            textReq.subscribe((data:any) => {
                var currentText = JSON.parse(data)
                let fetchedText: TextItem = this.fetchTextObject(currentText.id)
                fetchedText.setText(currentText.text)
            })
        }
    }

    getAudio(audio:AudioItem,audioId){
        var audio_get_url = location.origin + "/api/Audio/"+ audioId + "/?userId=" + this.injectedUserId;
        const audioReq = this.http.get(audio_get_url)

        audioReq.subscribe((data:any) => {
            var currentAudio = JSON.parse(data)
            audio.setAudioUrl(currentAudio.audioUrl)
            audio.setTranscript(currentAudio.transcript)
        })
    }

    getAllAudio(){
        
        for (let audio of this.audio_items){

             var audio_get_url = location.origin + "/api/Audio/"+ audio.id + "/?userId=" + this.injectedUserId;
             const audioReq = this.http.get(audio_get_url)

             audioReq.subscribe((data:any) => {
                 var currentAudio = JSON.parse(data)
                 let fetchedAudio: AudioItem = this.fetchAudioObject(currentAudio.id)
                 fetchedAudio.setAudioUrl(currentAudio.audioUrl)
                 fetchedAudio.setTranscript(currentAudio.transcript)
             })
         }
         this.loaded = true
    }


    fetchImageObject(imageId : string){
        for (let image of this.image_items){
            if (image.getId() == imageId){
                return image
            }
        }
    }

    fetchTextObject(textId :  string){
        for(let text of this.text_items){
            if(text.getId() == textId){
                return text
            }
        }
    }

    fetchAudioObject(audioId: string){
        for(let audio of this.audio_items){
            if(audio.getId() == audioId){
                return audio
            }
        }
    }

    resetErrorMessages(){
       this.imageErrorMessage = ""
       this.audioErrorMessage = ""
    }

    updateFile(event) {
        this.resetErrorMessages();
        this.showAudio = false
        if(event.target.files && event.target.files.length > 0) {         
          let file = event.target.files[0];
          
          //Read in separate formats for ease of UI rendering and blob storage uploads
          let result:boolean = this.readFileAsURL(file)
          if(result){
            this.readImageAsArrayBuffer(file)
          }
        }
    }

    updateAudioFile(event){
        this.resetErrorMessages()
        this.showSubmit = false
        if(event.target.files && event.target.files.length > 0) {         
            let file = event.target.files[0];
            
            //Read in separate formats for ease of UI rendering and blob storage uploads
            let result:boolean = this.readAudioFileAsURL(file)
            if(result){
                this.readAudioAsArrayBuffer(file)
            }
          }
    }

    readAudioFileAsURL(file:any){
        let urlReader = new FileReader();
        
        let fileExt:string = file.name.split('.').pop();

        if ((fileExt != "wav") || (fileExt === file.name)){
            this.audioErrorMessage = "Please upload an audio file with a .WAV extension"
            this.showAudio = false
            return false
        }
        else{
            this.audioFileName = file.name
            this.audioMimeType = 'audio/' + fileExt
            urlReader.readAsDataURL(file)
            urlReader.onload = (e:any) => {
                this.previewAudio = e.target.result
            }
            this.showAudio = true
            return true
        }

    }

    readAudioAsArrayBuffer(file){
        let bufferReader = new FileReader();
        bufferReader.readAsArrayBuffer(file);
        bufferReader.onload = (e:any) => {
            this.audioErrorMessage = ""
            this.audioAsArrayBuffer = e.target.result
        }
    }

    readFileAsURL(file:any){

        let urlReader = new FileReader();
        
        let fileExt:string = file.name.split('.').pop();

        if (((fileExt != "jpg") && (fileExt != "jpeg") && (fileExt != "png")||fileExt === file.name)){
            this.imageErrorMessage = "Please upload an image file with either a .PNG or .JPG extension"
            this.showSubmit = false
            this.previewImage = "#"
            return false
        }
        else{

            // Initialize mimetype
            if (fileExt === "jpg"){
                fileExt = "jpeg"
            }
            this.imageMimeType = 'image/' + fileExt
            urlReader.readAsDataURL(file)
            urlReader.onload = (e:any) => {
                this.previewImage = e.target.result
            }
            this.showSubmit = true
            return true
        }
    }

    readImageAsArrayBuffer(file:any){

        let bufferReader = new FileReader();
        bufferReader.readAsArrayBuffer(file);
        bufferReader.onload = (e:any) => {
            this.imageErrorMessage = ""
            this.imageAsArrayBuffer = e.target.result
        }
    }

    onImageFileSubmit(){
        var url = location.origin + "/api/Image?userId=" + this.injectedUserId;
        this.spinnerMessage = "Your image is being processed..."
        const req = this.http.post(url,{})
        req.subscribe(
            (res: any) => {
                
                var respJson = JSON.parse((res))
                let newImage:ImageItem = new ImageItem(respJson.id)
                this.image_items.push(newImage)
                this.loaded = true
                this.uploadToBlobStorage(respJson.id,respJson.url)
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    onAudioFileSubmit(){
        var url = location.origin + "/api/Audio?userId=" + this.injectedUserId;
        this.spinnerMessage = "Your audio is being processed..."
        const req = this.http.post(url,{})
        req.subscribe(
            (res: any) => {
                
                var respJson = JSON.parse((res))
                let newAudio:AudioItem = new AudioItem(respJson.id)
                this.audio_items.push(newAudio)
                this.loaded = true
                this.uploadAudioToBlobStorage(respJson.id,respJson.url)
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    uploadAudioToBlobStorage(id: string,url:string){

        const httpOptions = {
            headers: new HttpHeaders({
            'x-ms-blob-type':  'BlockBlob',
            'x-ms-blob-content-type': this.audioMimeType      
           })
       };
        const req = this.http.put(url,this.audioAsArrayBuffer, httpOptions )
        req.subscribe(
            (res:any) => {
                console.log("Successfully uploaded to Blob Service!")
                var createAudioUrl = location.origin + "/api/Audio/"+ id +"/?userId=" + this.injectedUserId;
                const audioReq = this.http.post(createAudioUrl,{ categoryId: this.injectedCategoryId })
                audioReq.subscribe(
                    (res: any) => {
                        let audioObj:AudioItem = this.fetchAudioObject(id)
                        console.log("Successfully created audio")
                        this.showHorizontalSpinner = false
                    },
                err => {
                    console.log("Received error while creating the image: " + err)
                })
            },
            err => {
                console.log("Received error while uploading to blob service: " + err)
            }
        )
    }

    uploadToBlobStorage(id: string,url:string){

        const httpOptions = {
            headers: new HttpHeaders({
            'x-ms-blob-type':  'BlockBlob',
            'x-ms-blob-content-type': this.imageMimeType      
           })
       };
        const req = this.http.put(url,this.imageAsArrayBuffer, httpOptions )
        req.subscribe(
            (res:any) => {
                console.log("Successfully uploaded to Blob Service!")
                var createImageUrl = location.origin + "/api/Image/"+ id +"/?userId=" + this.injectedUserId;
                const imageReq = this.http.post(createImageUrl,{ categoryId: this.injectedCategoryId })
                imageReq.subscribe(
                    (res: any) => {
                        var r_json = JSON.parse((res))
                        let previewUrl:string = r_json.previewUrl
                        let imageObj:ImageItem = this.fetchImageObject(id)
                        imageObj.setPreviewUrl(previewUrl)
                        this.showHorizontalSpinner = false
                        console.log("Successfully created image")
                    },
                err => {
                    console.log("Received error while creating the image: " + err)
                })
            },
            err => {
                console.log("Received error while uploading to blob service: " + err)
            }
        )
    }

    onClickDeleteImage(imageId:string){
        
        this.spinnerMessage = "Your image is being deleted..."
        var image_delete_url = location.origin + "/api/Image/"+ imageId + "/?userId=" + this.injectedUserId;
        const req = this.http.delete(image_delete_url)
        req.subscribe(
            (res: any) => {
                var index = this.itemIndexOf(imageId,this.image_items)
                if(index >-1){
                    this.image_items.splice(index,1)
                }
                this.showHorizontalSpinner = false
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    onClickDeleteAudio(audioId:string){
        
        this.spinnerMessage = "Your audio is being deleted..."
        var audio_delete_url = location.origin + "/api/Audio/"+ audioId + "/?userId=" + this.injectedUserId;
        const req = this.http.delete(audio_delete_url)
        req.subscribe(
            (res: any) => {
                var index = this.itemIndexOf(audioId,this.audio_items)
                if(index >-1){
                    this.audio_items.splice(index,1)
                }
                this.showHorizontalSpinner = false
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    itemIndexOf(item_id:string, arr:Array<any>){
        for (var i = 0; i < arr.length; i++) {
            if (arr[i].id === item_id){
                return i;
            }
        }
    }

    /** Text API's */
    
    onClickSubmitText(){
        this.spinnerMessage = "Your note is being created..."
        var url = location.origin + "/api/Text" + "?userId=" + this.injectedUserId;
        const req = this.http.post(url,{text: this.inputText,categoryId:this.injectedCategoryId} )
        req.subscribe(
            (res:any) => {
                var text = JSON.parse((res))
                let newText:TextItem = new TextItem(text.id)
                newText.setText(this.inputText)
                this.text_items.push(newText)
                this.showTextSubmit = false
                this.showHorizontalSpinner = false
            },
            err => {
                console.log("Received error while uploading to blob service: " + err)
            }
        )
    }

    onClickDeleteText(textId: string){

        this.spinnerMessage = "Your note is being deleted..."
        var text_delete_url = location.origin + "/api/Text/"+ textId + "/?userId=" + this.injectedUserId;
        const req = this.http.delete(text_delete_url)
        req.subscribe(
            (res: any) => {
                var index = this.itemIndexOf(textId,this.text_items)
                if(index >-1){
                    this.text_items.splice(index,1)
                }
                this.showHorizontalSpinner = false
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    onClickUpdateText(textId: string){
        var text_update_url = location.origin + "/api/Text/"+ textId + "/?userId=" + this.injectedUserId;
        const req = this.http.patch(text_update_url,{text: this.currentText})
        req.subscribe(
            (res: any) => {
                let text:TextItem = this.fetchTextObject(textId)
                text.setText(this.currentText)
            },
            err => {
                console.log("Received error response")
            }
        )
    }

    
    open(content,textId:string) {
        this.currentText = ""
        this.update = false
        let notes = this.fetchTextObject(textId).getText()
        this.currentText = notes
        this.modalService.open(content).result.then((result) => {        
            this.closeResult = `Closed with: ${result}`;
            }, (reason) => {
            this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
        });
    }

    openAudio(content,audioId:string){
        this.currentTranscript = ""
        let notes = this.fetchAudioObject(audioId).getTranscript()
        this.currentTranscript = notes
        this.modalService.open(content).result.then((result) => {        
            this.closeResult = `Closed with: ${result}`;
            }, (reason) => {
            this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
        });
    
    }
    
    openSubmitTextModal(content,textId:string) {
        this.inputText = ""
        this.modalService.open(content).result.then((result) => {        
            this.closeResult = `Closed with: ${result}`;
            }, (reason) => {
            this.closeResult = `Dismissed ${this.getDismissReason(reason)}`;
        });
     }

    private getDismissReason(reason: any): string {
        if (reason === ModalDismissReasons.ESC) {
        return 'by pressing ESC';
        } else if (reason === ModalDismissReasons.BACKDROP_CLICK) {
        return 'by clicking on a backdrop';
        } else {
        return  `with: ${reason}`;
        }
    }

    private onClickImageNotification(imageId: string){
        let image:ImageItem = this.fetchImageObject(imageId)
        image.showNotification = false
    }

    private onClickAudioNotification(audioId: string){
        let audio:AudioItem = this.fetchAudioObject(audioId)
        audio.showNotification = false
    }

    private onClickTextNotification(textId: string){
        let text:TextItem = this.fetchTextObject(textId)
        text.showNotification = false
    }

    logout(){

        // Disconnect user from signalR hub
        this.hub.getHubConnection().stop().then( () => {
            console.log("Connection stopped successfully")
            this.data.setCurrentUser("default")
            this.router.navigateByUrl('')
        }).catch(err => console.log("Error while establishing connection"));
        
    }
}
